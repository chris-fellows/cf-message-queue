using CFConnectionMessaging.Interfaces;
using CFConnectionMessaging.Models;
using CFMessageQueue.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CFMessageQueue.MessageConverters
{
    public class MessageQueueNotificationMessageConverter : IExternalMessageConverter<MessageQueueNotificationMessage>
    {
        public ConnectionMessage GetConnectionMessage(MessageQueueNotificationMessage externalMessage)
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
                         Name = "EventName",
                         Value = externalMessage.EventName
                     },
                     new ConnectionMessageParameter()   
                     {
                         Name = "QueueSize",
                         Value = externalMessage.QueueSize == null ? "" : externalMessage.QueueSize!.ToString()
                     }                   
                }
            };
            return connectionMessage;
        }

        public MessageQueueNotificationMessage GetExternalMessage(ConnectionMessage connectionMessage)
        {
            var externalMessage = new MessageQueueNotificationMessage()
            {
                Id = connectionMessage.Id,
                SecurityKey = connectionMessage.Parameters.First(p => p.Name == "SecurityKey").Value,
                EventName = connectionMessage.Parameters.First(p => p.Name == "EventName").Value,                
            };

            var queueSizeParamValue = connectionMessage.Parameters.First(p => p.Name == "QueueSize").Value;
            if (!String.IsNullOrEmpty(queueSizeParamValue))
            {
                externalMessage.QueueSize = Convert.ToInt64(queueSizeParamValue);
            }

            return externalMessage;
        }
    }
}
