﻿using CFMessageQueue.Models;
using CFMessageQueue.Services;

namespace CFMessageQueue.TestClient
{
    internal class MessageQueueTest
    {
        public async Task SendQueueMessage(MessageQueue messageQueue, string securityKey, int localPort)
        {
            using (var messageQueueClientConnector = new MessageQueueClientConnector(securityKey, localPort))
            {
                //var contentSerializer = new QueueMessageContentSerializer();

                // Set current message queue
                messageQueueClientConnector.MessageQueue = messageQueue;

                // Create test object to send
                var testObject = new TestObject()
                {
                    Id = Guid.NewGuid().ToString(),
                    DateTimeValue = DateTime.UtcNow,
                    BooleanValue = true,
                    Int32Value = 2000,
                    Int64Value = 29383837474
                };

                var queueMessage = new NewQueueMessage()
                {                    
                    TypeId = "TestMessage1",
                    Name = "My Test Message",
                    ExpirySeconds = 3600 * 24 * 7,
                    Priority = 50,
                    Content = testObject                 
                };

                await messageQueueClientConnector.SendAsync(queueMessage);
            }
        }

        public async Task<QueueMessage?> GetNextQueueMessage(MessageQueue messageQueue, string securityKey, int localPort)
        {
            using (var messageQueueClientConnector = new MessageQueueClientConnector(securityKey, localPort))
            {
                var contentSerializer = new QueueMessageContentSerializer();

                // Set current message queue
                messageQueueClientConnector.MessageQueue = messageQueue;

                var queueMessage = await messageQueueClientConnector.GetNextAsync(TimeSpan.Zero, TimeSpan.FromSeconds(300));

                //if (queueMessage != null)
                //{                    
                //    var newObject = contentSerializer.Deserialize(queueMessage.Content, Type.GetType(queueMessage.ContentType));
                //    int xxxxxx = 1000;
                //}
               
                int xxx = 1000;

                return queueMessage;
            }
        }
    }
}
