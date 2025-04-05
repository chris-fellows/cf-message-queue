using CFMessageQueue.Services;
using CFMessageQueue.TestClient.Extensions;
using CFMessageQueue.TestClient.Models;
using CFMessageQueue.Utilities;
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
            Console.Write("Consumer has started");

            var cancellationToken = _cancellationTokenSource.Token;

            var usedPorts= new List<int>();

            var hubClientLocalPort = NetworkUtilities.GetFreeLocalPort(SystemConfig.MinClientLocalPort, SystemConfig.MaxClientLocalPort, usedPorts);
            usedPorts.Add(hubClientLocalPort);
            var messageHubClientConnector = new MessageHubClientConnector(_consumerConfig.HubEndpointInfo,
                                     _consumerConfig.DefaultSecurityKey, hubClientLocalPort);                                     

            // Get message queue Id from name
            var messageQueues = messageHubClientConnector.GetMessageQueuesAsync().Result;
            var messageQueue = messageQueues.FirstOrDefault(mq => mq.Name == _consumerConfig.MessageQueueName);

            // Create message queue client
            var queueClientLocalPort = NetworkUtilities.GetFreeLocalPort(SystemConfig.MinClientLocalPort, SystemConfig.MaxClientLocalPort, usedPorts);
            usedPorts.Add(queueClientLocalPort);
            var messageQueueClientConnector = new MessageQueueClientConnector(_consumerConfig.DefaultSecurityKey, queueClientLocalPort);
            
            messageQueueClientConnector.SetMessageQueue(messageQueue);
            
            // Run until canceller
            while (!cancellationToken.IsCancellationRequested)
            {
                var queueMessage = messageQueueClientConnector.GetNextAsync(TimeSpan.Zero).Result;

                if (queueMessage == null)
                {
                    Console.WriteLine($"No message got from {messageQueue.Name}");
                }
                else
                {
                    Console.WriteLine($"Processing message {queueMessage.Id} from queue {messageQueue.Name}");

                    Console.WriteLine($"Setting message {queueMessage.Id} as processed");
                    messageQueueClientConnector.SetProcessed(queueMessage.Id, true).Wait();
                    Console.WriteLine($"Set message {queueMessage.Id} as processed");
                }

                // Delay before next get
                var stopwatch = new Stopwatch();
                stopwatch.Wait(_consumerConfig.DelayBetweenGetMessage, cancellationToken);
            }

            Console.WriteLine("Producer has completed");
        }
    }
}
