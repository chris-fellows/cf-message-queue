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
    public class GetQueueMessagesRequestConverter : IExternalMessageConverter<GetQueueMessagesRequest>
    {
        public ConnectionMessage GetConnectionMessage(GetQueueMessagesRequest externalMessage)
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
                       Name = "PageItems",
                       Value = externalMessage.PageItems.ToString()
                   },
                          new ConnectionMessageParameter()
                   {
                       Name = "Page",
                       Value = externalMessage.Page.ToString()
                   },
                }
            };
            return connectionMessage;
        }

        public GetQueueMessagesRequest GetExternalMessage(ConnectionMessage connectionMessage)
        {
            var externalMessage = new GetQueueMessagesRequest()
            {
                Id = connectionMessage.Id,
                SecurityKey = connectionMessage.Parameters.First(p => p.Name == "SecurityKey").Value,
                ClientSessionId = connectionMessage.Parameters.First(p => p.Name == "ClientSessionId").Value,
                MessageQueueId = connectionMessage.Parameters.First(p => p.Name == "MessageQueueId").Value,
                PageItems = Convert.ToInt32(connectionMessage.Parameters.First(p => p.Name == "PageItems").Value),
                Page = Convert.ToInt32(connectionMessage.Parameters.First(p => p.Name == "Page").Value)
            };

            return externalMessage;
        }
    }
}
