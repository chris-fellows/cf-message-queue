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
    /// Producer test
    /// </summary>
    internal class ProducerTest
    {
        public void Run(TimeSpan duration)
        {
            // Set producers
            var producerConfigs = new List<ProducerConfig>()
            {
                new ProducerConfig()
                {
                    MessageQueueName = "ProducerConsumerQueue1",
                    ClientName = SystemConfig.Client1Name,
                    AdminSecurityKey = SystemConfig.AdminSecurityKey,
                    DefaultSecurityKey = SystemConfig.Client1SecurityKey,
                    DelayBetweenSend = TimeSpan.FromMilliseconds(200),
                    HubEndpointInfo = SystemConfig.HubEndpointInfo,
                    HubLocalPort = 10160,
                    QueueLocalPort = 10161
                }
            };         
           
            var producers = new List<Producer>();

            // Configure system
            Configure(producerConfigs);

            // Start producers
            Console.WriteLine($"Starting {producerConfigs.Count} producers");
            foreach (var producerConfig in producerConfigs)
            {
                var producer = new Producer();
                producers.Add(producer);
                producer.Start(producerConfig, "Producer" + (producerConfigs.IndexOf(producerConfig) + 1).ToString());
            }

            // Run for specific udration
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            while (stopwatch.Elapsed < duration)
            {
                Thread.Sleep(100);
            }

            // Stop producer & consumer
            Console.WriteLine("Stopping producers");
            producers.ForEach(producer => producer.Stop());            
        }

        private void Configure(List<ProducerConfig> producerConfigs)
        {
            Console.WriteLine("Configuring for producers");

            // Default role types for queue functions
            var defaultQueueRoleTypes = RoleTypeUtilities.DefaultNonAdminQueueClientRoleTypes;

            // Default role types for hub functions
            var defaultHubRoleTypes = RoleTypeUtilities.DefaultNonAdminHubClientRoleTypes;

            var messageHubClientConnectorAdmin = new MessageHubClientConnector(producerConfigs[0].HubEndpointInfo,
                                     producerConfigs[0].AdminSecurityKey,
                                     NetworkUtilities.GetFreeLocalPort(SystemConfig.MinClientLocalPort, SystemConfig.MaxClientLocalPort, new()));

            // Create queue if not exists
            var messageQueues = messageHubClientConnectorAdmin.GetMessageQueuesAsync().Result;
            var messageQueue = messageQueues.FirstOrDefault(mq => mq.Name == producerConfigs[0].MessageQueueName);
            if (messageQueue == null)   // Queue not exists, create it
            {
                // Create message quite (Must be Admin)
                var messageQueueId = messageHubClientConnectorAdmin.AddMessageQueueAsync(producerConfigs[0].MessageQueueName, 5, 1000000).Result;

                // Get queue
                messageQueues = messageHubClientConnectorAdmin.GetMessageQueuesAsync().Result;
                messageQueue = messageQueues.FirstOrDefault(mq => mq.Name == producerConfigs[0].MessageQueueName);
            }

            // Clear queue (Must be Admin)
            messageHubClientConnectorAdmin.ClearMessageQueueAsync(messageQueue.Id).Wait();

            // Create producer client
            foreach (var producerConfig in producerConfigs)
            {
                var messageHubClientId1 = CreateMessageHubClient(messageHubClientConnectorAdmin, producerConfig.ClientName, producerConfig.DefaultSecurityKey,
                                            defaultHubRoleTypes, defaultQueueRoleTypes, new() { messageQueue.Id }).Result;
            }

            Console.WriteLine("Configured for producers");
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
                                                string name,
                                                string clientSecurityKey,
                                                List<RoleTypes> hubRoleTypes,
                                                List<RoleTypes> queueRoleTypes,
                                                List<string> messageQueueIds)
        {
            // Create message hub client            
            var messageHubClientId = await messageHubClientConnector.AddMessageHubClientAsync(name, clientSecurityKey);

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
