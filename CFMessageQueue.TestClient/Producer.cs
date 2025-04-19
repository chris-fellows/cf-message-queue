using CFMessageQueue.Models;
using CFMessageQueue.Services;
using CFMessageQueue.TestClient.Extensions;
using CFMessageQueue.TestClient.Models;
using CFMessageQueue.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace CFMessageQueue.TestClient
{
    /// <summary>
    /// Producer for messages
    /// </summary>
    internal class Producer
    {
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        private ProducerConfig? _producerConfig;
        private Thread? _thread;

        private string _id = "";

        public void Start(ProducerConfig producerConfig, string id)
        {
            if (_thread != null)
            {
                throw new ArgumentException("Producer is already running");
            }

            _id = id;
            _producerConfig = producerConfig;
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
            Console.WriteLine($"{_id}: Producer has started");
            
            var cancellationToken = _cancellationTokenSource.Token;

            var messageHubClientConnector = new MessageHubClientConnector(_producerConfig.HubEndpointInfo,
                                        _producerConfig.DefaultSecurityKey, _producerConfig.HubLocalPort);
                                        
            // Get message queue Id from name            
            var messageQueues = messageHubClientConnector.GetMessageQueuesAsync().Result;
            var messageQueue = messageQueues.FirstOrDefault(mq => mq.Name == _producerConfig.MessageQueueName);
            if (messageQueue == null)   // Queue not exists, create it
            {
                throw new ArgumentException($"Queue {_producerConfig.MessageQueueName} does not exist");
            }
            
            // Create message queue client            
            var messageQueueClientConnector = new MessageQueueClientConnector(_producerConfig.DefaultSecurityKey, _producerConfig.QueueLocalPort);

            // Set client current queue
            messageQueueClientConnector.MessageQueue = messageQueue;

            // Run until cancelled
            int countMessagesSent = 0;            
            while (!cancellationToken.IsCancellationRequested)
            {
                var testObject = new TestObject()
                {
                    Id = Guid.NewGuid().ToString(),
                    DateTimeValue = DateTime.UtcNow,
                    BooleanValue = true,
                    Int32Value = 2000,
                    Int64Value = 29383837474
                };

                // Send message
                var queueMessage = new NewQueueMessage()
                {                                        
                    TypeId = "Test1",
                    ExpirySeconds = 3600 * 24 * 7,
                    Name = $"Message {countMessagesSent + 1}",
                    Priority = 50,                   
                    Content = testObject
                };

                Console.WriteLine($"{_id}: Sending message {queueMessage.Name} to {messageQueue.Name}");
                messageQueueClientConnector.SendAsync(queueMessage).Wait();
                Console.WriteLine($"{_id}: Sent message");
                countMessagesSent++;

                // Delay before next send
                var stopwatch2 = new Stopwatch();
                stopwatch2.Wait(_producerConfig.DelayBetweenSend, cancellationToken);
            }

            // Clean up                  
            messageHubClientConnector.Dispose();
            messageQueueClientConnector.Dispose();

            Console.WriteLine($"{_id}: Producer has completed (Sent {countMessagesSent} messages)");
        }           
    }
}
