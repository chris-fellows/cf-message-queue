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
    public class GetMessageHubClientsRequestConverter : IExternalMessageConverter<GetMessageHubClientsRequest>
    {
        public ConnectionMessage GetConnectionMessage(GetMessageHubClientsRequest externalMessage)
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
                }
            };
            return connectionMessage;
        }

        public GetMessageHubClientsRequest GetExternalMessage(ConnectionMessage connectionMessage)
        {
            var externalMessage = new GetMessageHubClientsRequest()
            {
                Id = connectionMessage.Id,
                SecurityKey = connectionMessage.Parameters.First(p => p.Name == "SecurityKey").Value,
                ClientSessionId = connectionMessage.Parameters.First(p => p.Name == "ClientSessionId").Value                
            };

            return externalMessage;
        }
    }
}
