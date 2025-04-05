using CFConnectionMessaging.Interfaces;
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
    public class AddQueueMessageRequestConverter : IExternalMessageConverter<AddQueueMessageRequest>
    {
        public ConnectionMessage GetConnectionMessage(AddQueueMessageRequest externalMessage)
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
                          Name = "ClientSessionId",
                          Value = externalMessage.ClientSessionId
                      }
                        ,
                    new ConnectionMessageParameter()
                   {
                       Name = "MessageQueueId",
                       Value = externalMessage.MessageQueueId
                   },
                   new ConnectionMessageParameter()
                   {
                       Name = "QueueMessage",
                       Value = externalMessage.QueueMessage == null ? "" :
                                        JsonUtilities.SerializeToBase64String(externalMessage.QueueMessage,
                                        JsonUtilities.DefaultJsonSerializerOptions)
                   }
                }
            };
            return connectionMessage;
        }

        public AddQueueMessageRequest GetExternalMessage(ConnectionMessage connectionMessage)
        {
            var externalMessage = new AddQueueMessageRequest()
            {
                Id = connectionMessage.Id,
                SecurityKey = connectionMessage.Parameters.First(p => p.Name == "SecurityKey").Value,
                ClientSessionId = connectionMessage.Parameters.First(p => p.Name == "ClientSessionId").Value,
                MessageQueueId = connectionMessage.Parameters.First(p => p.Name == "MessageQueueId").Value,
            };         

            // Get queue message
            var queueMessageParameter = connectionMessage.Parameters.First(p => p.Name == "QueueMessage");
            if (!String.IsNullOrEmpty(queueMessageParameter.Value))
            {
                externalMessage.QueueMessage = JsonUtilities.DeserializeFromBase64String<QueueMessageInternal>(queueMessageParameter.Value, JsonUtilities.DefaultJsonSerializerOptions);
            }

            return externalMessage;
        }
    }
}
