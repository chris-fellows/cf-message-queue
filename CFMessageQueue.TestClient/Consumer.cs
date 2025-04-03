using CFMessageQueue.Services;
using CFMessageQueue.TestClient.Extensions;
using CFMessageQueue.TestClient.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CFMessageQueue.TestClient
{
    /// <summary>
    /// Consumer for messages
    /// </summary>
    internal class Consumer
    {
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        private ConsumerConfig? _consumerConfig;
        private Thread? _thread;

        public void Start(ConsumerConfig consumerConfig)
        {
            if (_thread != null)
            {
                throw new ArgumentException("Consumer is already running");
            }

            _consumerConfig = consumerConfig;
            _thread = new Thread(Run);
            _thread.Start();
        }

        public void Stop()
        {
            _cancellationTokenSource.Cancel();
            if (_thread != null)
            {
                _thread.Join();
                _thread = null;
            }
        }

        public void Run()
        {
            var cancellationToken = _cancellationTokenSource.Token;

            var messageHubClientConnector = new MessageHubClientConnector(_consumerConfig.HubEndpointInfo,
                                     _consumerConfig.AdminSecurityKey,
                                     _consumerConfig.DefaultSecurityKey,
                                     _consumerConfig.LocalPort);

            // Get message queue Id from name
            var messageQueues = messageHubClientConnector.GetMessageQueuesAsync().Result;
            var messageQueue = messageQueues.FirstOrDefault(mq => mq.Name == _consumerConfig.MessageQueueName);

            // Create message queue client
            var messageQueueClientConnector = new MessageQueueClientConnector(_consumerConfig.DefaultSecurityKey);
            messageQueueClientConnector.SetMessageQueue(messageQueue);

            var stopwatch = new Stopwatch();
            while (!cancellationToken.IsCancellationRequested)
            {
                var queueMessage = messageQueueClientConnector.GetNextAsync().Result;

                if (queueMessage == null)
                {
                    Console.WriteLine($"No message got from {messageQueue.Name}");
                }
                else
                {
                    Console.WriteLine($"Processing message {queueMessage.Id} from queue {messageQueue.Name}");
                }

                // Delay before next get
                stopwatch.Wait(_consumerConfig.DelayBetweenGetMessage, cancellationToken);
            }
        }
    }
}
