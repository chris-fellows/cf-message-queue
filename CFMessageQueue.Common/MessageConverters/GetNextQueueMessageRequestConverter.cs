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
    public class GetNextQueueMessageRequestConverter : IExternalMessageConverter<GetNextQueueMessageRequest>
    {
        public ConnectionMessage GetConnectionMessage(GetNextQueueMessageRequest externalMessage)
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
                       Name = "MaxWaitMilliseconds",
                       Value = externalMessage.MaxWaitMilliseconds.ToString()
                   },
                          new ConnectionMessageParameter()
                   {
                       Name = "MaxProcessingSeconds",
                       Value = externalMessage.MaxProcessingSeconds.ToString()
                   },
                }
            };
            return connectionMessage;
        }

        public GetNextQueueMessageRequest GetExternalMessage(ConnectionMessage connectionMessage)
        {
            var externalMessage = new GetNextQueueMessageRequest()
            {
                Id = connectionMessage.Id,
                SecurityKey = connectionMessage.Parameters.First(p => p.Name == "SecurityKey").Value,
                ClientSessionId = connectionMessage.Parameters.First(p => p.Name == "ClientSessionId").Value,
                MessageQueueId = connectionMessage.Parameters.First(p => p.Name == "MessageQueueId").Value,
                MaxWaitMilliseconds = Convert.ToInt32(connectionMessage.Parameters.First(p => p.Name == "MaxWaitMilliseconds").Value),
                MaxProcessingSeconds = Convert.ToInt32(connectionMessage.Parameters.First(p => p.Name == "MaxProcessingSeconds").Value)
            };   

            return externalMessage;
        }
    }
}
