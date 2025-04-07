using CFConnectionMessaging.Models;
using CFMessageQueue.Enums;
using CFMessageQueue.Interfaces;
using CFMessageQueue.Models;
using CFMessageQueue.Services;
using CFMessageQueue.TestClient.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CFMessageQueue.TestClient
{
    internal class Configurer
    {
        public async Task<string> CreateMessageQueueAsync(EndpointInfo hubEndpointInfo, string name, int maxConcurrentProcessing, int maxSize, int localPort)
        {            
            using (var messageHubClientConnector = new MessageHubClientConnector(hubEndpointInfo, SystemConfig.AdminSecurityKey, localPort))
            {

                // Create message queue (Gives full permissions to admin hub client)
                var messageQueueId = await messageHubClientConnector.AddMessageQueueAsync(name, maxConcurrentProcessing, maxSize);

                return messageQueueId;
            }
        }     

        public async Task<string> CreateMessageHubClientAsync(EndpointInfo hubEndpointInfo, string messageQueueId, string name, string clientSecurityKey, int localPort)
        {            
            //var adminSecurityKey = "5005db05-35eb-4471-bd05-7883b746b196";
            //var defaultSecurityKey = "0b38818c-4354-43f5-a750-a24378d2e3a8";

            //int localPort = 10010;

            /*
            var remoteEndpointInfo = new EndpointInfo()
            {
                Ip = "192.168.1.45",
                Port = 10000
            };
            */
            using (var messageHubClientConnector = new MessageHubClientConnector(hubEndpointInfo, SystemConfig.AdminSecurityKey, localPort))
            {
                // Create message queue (Gives full permissions to admin hub client)
                //var messageQueueId = messageHubClientConnector.AddMessageQueueAsync(SystemConfig.QueueName1).Result;

                // Create message hub clients

                // Create message hub client            
                var messageHubClientId = await messageHubClientConnector.AddMessageHubClientAsync(name, clientSecurityKey);

                // Configure hub client for specific queue
                await messageHubClientConnector.ConfigureMessageHubClientAsync(messageHubClientId, messageQueueId,
                    new List<RoleTypes>()
                    {
                        //RoleTypes.ClearQueue,
                        RoleTypes.QueueReadQueue,
                        RoleTypes.QueueWriteQueue,
                        RoleTypes.QueueSubscribeQueue
                    });

                return messageHubClientId;
            }
        }
    }
}
