﻿using CFConnectionMessaging.Interfaces;
using CFConnectionMessaging.Models;
using CFMessageQueue.Models;
using CFMessageQueue.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CFMessageQueue.MessageConverters
{
    public class AddMessageHubClientRequestConverter : IExternalMessageConverter<AddMessageHubClientRequest>
    {
        public ConnectionMessage GetConnectionMessage(AddMessageHubClientRequest externalMessage)
        {
            var connectionMessage = new ConnectionMessage()
            {
                Id = externalMessage.Id,
                TypeId = externalMessage.TypeId,
                Parameters = new List<ConnectionMessageParameter>()
                {
                     new ConnectionMessageParameter()
                    {
                        Name = "SecurityKey",
                        Value = externalMessage.SecurityKey
                    },
                    new ConnectionMessageParameter()
                    {
                        Name = "Name",
                        Value = externalMessage.Name
                    },
                      new ConnectionMessageParameter()
                    {
                        Name = "ClientSecurityKey",
                        Value = externalMessage.ClientSecurityKey
                    },
                        new ConnectionMessageParameter()
                    {
                        Name = "ClientSessionId",
                        Value = externalMessage.ClientSessionId
                    }

                }
            };
            return connectionMessage;
        }

        public AddMessageHubClientRequest GetExternalMessage(ConnectionMessage connectionMessage)
        {
            var externalMessage = new AddMessageHubClientRequest()
            {
                Id = connectionMessage.Id,
                SecurityKey = connectionMessage.Parameters.First(p => p.Name == "SecurityKey").Value,
                Name = connectionMessage.Parameters.First(p => p.Name == "Name").Value,
                ClientSecurityKey = connectionMessage.Parameters.First(p => p.Name == "ClientSecurityKey").Value,
                ClientSessionId = connectionMessage.Parameters.First(p => p.Name == "ClientSessionId").Value
            };

            //// Get message queue
            //var messageQueueParameter = connectionMessage.Parameters.First(p => p.Name == "MessageQueue");
            //if (!String.IsNullOrEmpty(messageQueueParameter.Value))
            //{
            //    externalMessage.MessageQueue = JsonUtilities.DeserializeFromBase64String<MessageQueue>(messageQueueParameter.Value, JsonUtilities.DefaultJsonSerializerOptions);
            //}

            //// Get queue message
            //var queueMessageParameter = connectionMessage.Parameters.First(p => p.Name == "QueueMessage");
            //if (!String.IsNullOrEmpty(queueMessageParameter.Value))
            //{
            //    externalMessage.QueueMessage = JsonUtilities.DeserializeFromBase64String<QueueMessage>(queueMessageParameter.Value, JsonUtilities.DefaultJsonSerializerOptions);
            //}

            return externalMessage;
        }
    }
}
