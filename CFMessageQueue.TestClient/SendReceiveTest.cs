using CFMessageQueue.TestClient.Models;
using CFMessageQueue.Enums;
using CFMessageQueue.Services;
using CFMessageQueue.Utilities;
using System.Diagnostics;
using CFMessageQueue.Interfaces;
using CFMessageQueue.Models;
using System.Xml.Linq;

namespace CFMessageQueue.TestClient
{
    /// <summary>
    /// Runs a basic send and receive test.
    /// 
    /// Creates queue, hub clients, sends messages, gets next message, receives queue notifications.
    /// </summary>
    internal class SendReceiveTest
    {
        public async Task Run()
        {
            //var cancellationTokenSource = new CancellationTokenSource();

            // Default role types for queue functions
            var defaultQueueRoleTypes = RoleTypeUtilities.DefaultNonAdminQueueClientRoleTypes;

            // Default role types for hub functions
            var defaultHubRoleTypes = RoleTypeUtilities.DefaultNonAdminHubClientRoleTypes;

            var configurer = new Configurer();

            // Store local ports used so that we can identify free ports
            var localPortsUsed = new List<int>();

            // Create message hub client connector for admin functions. E.g. Create queue, create message hub clients
            var localPortHubClientConnectorAdmin = NetworkUtilities.GetFreeLocalPort(SystemConfig.MinClientLocalPort, SystemConfig.MaxClientLocalPort, localPortsUsed);
            localPortsUsed.Add(localPortHubClientConnectorAdmin);
            var messageHubClientConnectorAdmin = new MessageHubClientConnector(SystemConfig.HubEndpointInfo, SystemConfig.AdminSecurityKey,
                                                      localPortHubClientConnectorAdmin);

            // Create message queue                        
            Console.WriteLine($"Creating queue {SystemConfig.Queue1Name}");
            var messageQueueId = await messageHubClientConnectorAdmin.AddMessageQueueAsync(SystemConfig.Queue1Name, 5, 10000);
            Console.WriteLine($"Creating queue (Id={messageQueueId})");

            //await messageHubClientConnectorAdmin.ConfigureMessageHubClientAsync(messageHubClientId, messageQueueId, queueRoleTypes);

            // Create client 1 with hub & queue permissions
            var messageHubClientId1 = await CreateMessageHubClient(messageHubClientConnectorAdmin, SystemConfig.Client1SecurityKey,
                                            defaultHubRoleTypes, defaultQueueRoleTypes, new() { messageQueueId });

            // Create client 2 with hub & queue permissions
            var messageHubClientId2 = await CreateMessageHubClient(messageHubClientConnectorAdmin, SystemConfig.Client2SecurityKey,
                                            defaultHubRoleTypes, defaultQueueRoleTypes, new() { messageQueueId });

            // Clean up admin hub client connector so that we error below if we try and use the object
            messageHubClientConnectorAdmin.Dispose();
            messageHubClientConnectorAdmin = null;

            // Create message hub client connector 1
            var localPortHubClientConnector1 = NetworkUtilities.GetFreeLocalPort(SystemConfig.MinClientLocalPort, SystemConfig.MaxClientLocalPort, localPortsUsed);
            localPortsUsed.Add(localPortHubClientConnector1);
            var messageHubClientConnector1 = new MessageHubClientConnector(SystemConfig.HubEndpointInfo, SystemConfig.Client1SecurityKey, localPortHubClientConnector1);
            
            // Create message queue client connector 1            
            var localPortQueueClientConnector1 = NetworkUtilities.GetFreeLocalPort(SystemConfig.MinClientLocalPort, SystemConfig.MaxClientLocalPort, localPortsUsed);
            localPortsUsed.Add(localPortQueueClientConnector1);
            var messageQueueClientConnector1 = new MessageQueueClientConnector(SystemConfig.Client1SecurityKey, localPortQueueClientConnector1);

            // Create message queue client connector 2
            var localPortQueueClientConnector2 = NetworkUtilities.GetFreeLocalPort(SystemConfig.MinClientLocalPort, SystemConfig.MaxClientLocalPort, localPortsUsed);
            localPortsUsed.Add(localPortQueueClientConnector2);
            var messageQueueClientConnector2 = new MessageQueueClientConnector(SystemConfig.Client1SecurityKey, localPortQueueClientConnector2);

            // Get message queues
            Console.WriteLine("Getting messages queues");
            var messageQueues = messageHubClientConnector1.GetMessageQueuesAsync().Result;
            Console.WriteLine($"Got {messageQueues.Count} messages queues");

            // Get new message queue
            var messageQueue = messageQueues.First(q => q.Id == messageQueueId);

            // Set current message queue
            messageQueueClientConnector1.MessageQueue = messageQueue;
            messageQueueClientConnector2.MessageQueue = messageQueue;
           
            // Subscribe on client 2 to receive notifications
            var subscribeId = await messageQueueClientConnector2.SubscribeAsync((eventName, queueSize) =>
            {
                if (queueSize == null)
                {
                    Console.WriteLine($"Queue notification: Event Name={eventName}");
                }
                else
                {
                    Console.WriteLine($"Queue notification: Event Name={eventName}, Queue Size={queueSize.Value}");
                }
            }, TimeSpan.FromSeconds(20));

            // Send test message from client 1
            Console.WriteLine("Sending messages to queue");
            await SendQueueMessageTestObjectAsync(messageQueueClientConnector1);

            // Test getting message
            Console.WriteLine("Getting next message from queue");
            var queueMessage = messageQueueClientConnector1.GetNextAsync(TimeSpan.Zero);

            if (queueMessage == null)
            {
                Console.WriteLine("Got no next message from queue");
            }
            else
            {
                Console.WriteLine($"Got next message {queueMessage.Id} from queuee");
            }

            // Wait for at least 30 secs for queue notifications
            Console.WriteLine("Waiting for queue notifications");
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            while (stopwatch.Elapsed < TimeSpan.FromSeconds(30))
            {
                Thread.Sleep(100);
            }

            Console.WriteLine("Cleaning up");

            // Clean up before we leave
            messageHubClientConnector1.Dispose();
            messageQueueClientConnector1.Dispose();
            messageQueueClientConnector2.Dispose();

            // Cancel tasks (E.g. Queue notifications)
            //cancellationTokenSource.Cancel();
            int xxx = 1000;
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

        private async Task SendQueueMessageTestObjectAsync(IMessageQueueClientConnector messageQueueClientConnector)
        {
            // Create test object to send
            var testObject = new TestObject()
            {
                Id = Guid.NewGuid().ToString(),
                DateTimeValue = DateTime.UtcNow,
                BooleanValue = true,
                Int32Value = 2000,
                Int64Value = 29383837474
            };

            var queueMessage = new QueueMessage()
            {
                Id = Guid.NewGuid().ToString(),
                CreatedDateTime = DateTimeOffset.UtcNow,
                TypeId = "TestMessage1",
                Content = testObject
                //ContentType = testObject.GetType().AssemblyQualifiedName,
                //Content = contentSerializer.Serialize(testObject, testObject.GetType())
            };

            await messageQueueClientConnector.SendAsync(queueMessage);
        }
    }
}
