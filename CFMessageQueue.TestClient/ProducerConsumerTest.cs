using CFMessageQueue.Enums;
using CFMessageQueue.Interfaces;
using CFMessageQueue.Services;
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
    /// Producer / Consumer test.
    /// </summary>
    internal class ProducerConsumerTest
    {
        public void Run(TimeSpan duration)
        {
            var producerConfig = new ProducerConfig()
            {
                MessageQueueName = "ProducerConsumerQueue1",
                AdminSecurityKey = SystemConfig.AdminSecurityKey,
                DefaultSecurityKey = SystemConfig.Client1SecurityKey,
                DelayBetweenSend = TimeSpan.FromSeconds(5),
                HubEndpointInfo = SystemConfig.HubEndpointInfo,               
            };
            
            var consumerConfig = new ConsumerConfig()
            {
                MessageQueueName = "ProducerConsumerQueue1",
                AdminSecurityKey = SystemConfig.AdminSecurityKey,
                DefaultSecurityKey= SystemConfig.Client2SecurityKey,
                DelayBetweenGetMessage = TimeSpan.FromSeconds(3),
                HubEndpointInfo = SystemConfig.HubEndpointInfo
            };

            // Configure system
            Configure(consumerConfig, producerConfig);

            // Start producer
            var producer = new Producer();
            producer.Start(producerConfig);

            // Start consumer
            var consumer =new Consumer();
            consumer.Start(consumerConfig);

            // Run for specific udration
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            while (stopwatch.Elapsed < duration)
            {
                Thread.Sleep(100);
            }

            // Stop producer & consumer
            producer.Stop();
            consumer.Stop();
        }

        private void Configure(ConsumerConfig consumerConfig, ProducerConfig producerConfig)
        { 
            // Default role types for queue functions
            var defaultQueueRoleTypes = new List<RoleTypes>()
            {
                RoleTypes.ReadQueue,
                RoleTypes.WriteQueue,
                RoleTypes.SubscribeQueue
            };

            // Default role types for hub functions
            var defaultHubRoleTypes = new List<RoleTypes>()
            {
                RoleTypes.GetMessageHubs,
                RoleTypes.GetMessageQueues
            };

            var messageHubClientConnectorAdmin = new MessageHubClientConnector(producerConfig.HubEndpointInfo,
                                     producerConfig.AdminSecurityKey,
                                     NetworkUtilities.GetFreeLocalPort(SystemConfig.MinClientLocalPort, SystemConfig.MaxClientLocalPort, new()));

            // Create queue if not exists
            var messageQueues = messageHubClientConnectorAdmin.GetMessageQueuesAsync().Result;
            var messageQueue = messageQueues.FirstOrDefault(mq => mq.Name == producerConfig.MessageQueueName);
            if (messageQueue == null)   // Queue not exists, create it
            {
                // Create message quite (Must be Admin)
                var messageQueueId = messageHubClientConnectorAdmin.AddMessageQueueAsync(producerConfig.MessageQueueName, 5, 1000000).Result;

                // Get queue
                messageQueues = messageHubClientConnectorAdmin.GetMessageQueuesAsync().Result;
                messageQueue = messageQueues.FirstOrDefault(mq => mq.Name == producerConfig.MessageQueueName);
            }

            // Clear queue (Must be Admin)
            messageHubClientConnectorAdmin.ClearMessageQueueAsync(messageQueue.Id).Wait();

            // Create producer client
            var messageHubClientId1 = CreateMessageHubClient(messageHubClientConnectorAdmin, producerConfig.DefaultSecurityKey,
                                        defaultHubRoleTypes, defaultQueueRoleTypes, new() { messageQueue.Id }).Result;

            // Create consumer client
            var messageHubClientId2 = CreateMessageHubClient(messageHubClientConnectorAdmin, consumerConfig.DefaultSecurityKey, 
                                        defaultHubRoleTypes, defaultQueueRoleTypes, new() { messageQueue.Id }).Result;
        }        

        /// <summary>
        /// Creates message hub client and sets hub and queue level permissions
        /// </summary>
        /// <param name="messageHubClientConnector"></param>
        /// <param name="clientSecurityKey"></param>
        /// <param name="hubRoleTypes"></param>
        /// <param name="queueRoleTypes"></param>
        /// <param name="messageQueueIds"></param>
        /// <returns></returns>
        private async Task<string> CreateMessageHubClient(IMessageHubClientConnector messageHubClientConnector,
                                                string clientSecurityKey,
                                                List<RoleTypes> hubRoleTypes,
                                                List<RoleTypes> queueRoleTypes,
                                                List<string> messageQueueIds)
        {
            // Create message hub client            
            var messageHubClientId = await messageHubClientConnector.AddMessageHubClientAsync(clientSecurityKey);

            // Configure hub level permissions            
            await messageHubClientConnector.ConfigureMessageHubClientAsync(messageHubClientId, hubRoleTypes);

            // Configure queue level permissions            
            foreach (var messageQueueId in messageQueueIds)
            {
                await messageHubClientConnector.ConfigureMessageHubClientAsync(messageHubClientId, messageQueueId, queueRoleTypes);
            }

            return messageHubClientId;
        }
    }
}
