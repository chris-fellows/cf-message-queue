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
    public class MessageQueueSubscribeRequestConverter : IExternalMessageConverter<MessageQueueSubscribeRequest>
    {
        public ConnectionMessage GetConnectionMessage(MessageQueueSubscribeRequest externalMessage)
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
                    //  new ConnectionMessageParameter()
                    //{
                    //    Name = "SenderAgentId",
                    //    Value = externalMessage.SenderAgentId
                    //}
                    new ConnectionMessageParameter()
                   {
                       Name = "MessageQueueId",
                       Value = externalMessage.MessageQueueId
                   },
                }
            };
            return connectionMessage;
        }

        public MessageQueueSubscribeRequest GetExternalMessage(ConnectionMessage connectionMessage)
        {
            var externalMessage = new MessageQueueSubscribeRequest()
            {
                Id = connectionMessage.Id,
                SecurityKey = connectionMessage.Parameters.First(p => p.Name == "SecurityKey").Value,
                MessageQueueId = connectionMessage.Parameters.First(p => p.Name == "MessageQueueId").Value
            };

            return externalMessage;
        }
    }
}
