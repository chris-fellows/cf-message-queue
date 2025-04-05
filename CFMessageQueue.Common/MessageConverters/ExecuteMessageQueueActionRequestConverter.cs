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
    public class ExecuteMessageQueueActionRequestConverter : IExternalMessageConverter<ExecuteMessageQueueActionRequest>
    {
        public ConnectionMessage GetConnectionMessage(ExecuteMessageQueueActionRequest externalMessage)
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
  },
                      new ConnectionMessageParameter()
                    {
                        Name = "MessageQueueId",
                        Value = externalMessage.MessageQueueId
                    },
                         new ConnectionMessageParameter()
                    {
                        Name = "ActionName",
                        Value = externalMessage.ActionName
                    }

                   // new ConnectionMessageParameter()
                   //{
                   //    Name = "MessageQueue",
                   //    Value = externalMessage.MessageQueue == null ? "" :
                   //                     JsonUtilities.SerializeToBase64String(externalMessage.MessageQueue,
                   //                     JsonUtilities.DefaultJsonSerializerOptions)
                   //},
                   //new ConnectionMessageParameter()
                   //{
                   //    Name = "QueueMessage",
                   //    Value = externalMessage.QueueMessage == null ? "" :
                   //                     JsonUtilities.SerializeToBase64String(externalMessage.QueueMessage,
                   //                     JsonUtilities.DefaultJsonSerializerOptions)
                   //}
                }
            };
            return connectionMessage;
        }

        public ExecuteMessageQueueActionRequest GetExternalMessage(ConnectionMessage connectionMessage)
        {
            var externalMessage = new ExecuteMessageQueueActionRequest()
            {
                Id = connectionMessage.Id,
                SecurityKey = connectionMessage.Parameters.First(p => p.Name == "SecurityKey").Value,
                ClientSessionId = connectionMessage.Parameters.First(p => p.Name == "ClientSessionId").Value,
                MessageQueueId = connectionMessage.Parameters.First(p => p.Name == "MessageQueueId").Value,
                ActionName = connectionMessage.Parameters.First(p => p.Name == "ActionName").Value,
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
