using CFMessageQueue.Models;
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
    /// Producer for messages
    /// </summary>
    internal class Producer
    {
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        private ProducerConfig? _producerConfig;
        private Thread? _thread;

        public void Start(ProducerConfig producerConfig)
        {
            if (_thread != null)
            {
                throw new ArgumentException("Producer is already running");
            }

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
            var cancellationToken = _cancellationTokenSource.Token;

            var messageHubClientConnector = new MessageHubClientConnector(_producerConfig.HubEndpointInfo, 
                                        _producerConfig.AdminSecurityKey, 
                                        _producerConfig.DefaultSecurityKey, 
                                        _producerConfig.LocalPort);

            // Get message queue Id from name
            var messageQueues = messageHubClientConnector.GetMessageQueuesAsync().Result;
            var messageQueue = messageQueues.FirstOrDefault(mq => mq.Name == _producerConfig.MessageQueueName);

            // Create message queue client
            var messageQueueClientConnector = new MessageQueueClientConnector(_producerConfig.DefaultSecurityKey, _producerConfig.LocalPort);
            messageQueueClientConnector.SetMessageQueue(messageQueue);

            var stopwatch = new Stopwatch();
            while (!cancellationToken.IsCancellationRequested)
            {
                // Send message
                var queueMessage = new QueueMessage()
                {
                    Id = Guid.NewGuid().ToString(),
                    CreatedDateTime = DateTimeOffset.UtcNow,
                    TypeId = "Test1"                     
                };
                messageQueueClientConnector.SendAsync(queueMessage).Wait();

                Console.WriteLine($"Sending message {queueMessage.Id} to {messageQueue.Name}");

                // Delay before next send                
                stopwatch.Wait(_producerConfig.DelayBetweenSend, cancellationToken);
            }
        }           
    }
}
