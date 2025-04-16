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

        private string _id = "";

        public void Start(ConsumerConfig consumerConfig, string id)
        {
            if (_thread != null)
            {
                throw new ArgumentException("Consumer is already running");
            }

            _id = id;
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
            Console.Write($"{_id}: Consumer has started");

            var cancellationToken = _cancellationTokenSource.Token;

            var usedPorts= new List<int>();
            
            var messageHubClientConnector = new MessageHubClientConnector(_consumerConfig.HubEndpointInfo,
                                     _consumerConfig.DefaultSecurityKey, _consumerConfig.HubLocalPort);                                     

            // Get message queue Id from name
            var messageQueues = messageHubClientConnector.GetMessageQueuesAsync().Result;
            var messageQueue = messageQueues.FirstOrDefault(mq => mq.Name == _consumerConfig.MessageQueueName);

            // Create message queue client            
            var messageQueueClientConnector = new MessageQueueClientConnector(_consumerConfig.DefaultSecurityKey, _consumerConfig.QueueLocalPort);

            // Set current message queue
            messageQueueClientConnector.MessageQueue = messageQueue;

            // Run until canceller
            int countMessagesProcessed = 0;
            while (!cancellationToken.IsCancellationRequested)
            {
                Console.WriteLine($"{_id}: Getting next message from queue {messageQueue.Name}"); ;
                var queueMessage = messageQueueClientConnector.GetNextAsync(TimeSpan.Zero, TimeSpan.FromSeconds(300)).Result;

                if (queueMessage == null)
                {
                    Console.WriteLine($"{_id}: No message got from {messageQueue.Name}");
                }
                else
                {
                    countMessagesProcessed++;
                    Console.WriteLine($"{_id}: Got message {queueMessage.Id} (Processed={countMessagesProcessed})");

                    Console.WriteLine($"{_id}: Setting message {queueMessage.Id} as processed");
                    messageQueueClientConnector.SetProcessed(queueMessage.Id, true).Wait();
                    Console.WriteLine($"{_id}: Set message {queueMessage.Id} as processed");
                }

                // Delay before next get
                var stopwatch = new Stopwatch();
                stopwatch.Wait(_consumerConfig.DelayBetweenGetMessage, cancellationToken);
            }

            Console.WriteLine($"{_id}: Consumer has completed (Processed={countMessagesProcessed})");
        }
    }
}
