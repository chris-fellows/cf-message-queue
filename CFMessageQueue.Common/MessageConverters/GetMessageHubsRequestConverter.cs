using CFConnectionMessaging.Interfaces;
using CFConnectionMessaging.Models;
using CFMessageQueue.Constants;
using CFMessageQueue.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CFMessageQueue.MessageConverters
{
    public class GetMessageHubsRequestConverter : IExternalMessageConverter<GetMessageHubsRequest>
    {
        public ConnectionMessage GetConnectionMessage(GetMessageHubsRequest externalMessage)
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
                }
            };
            return connectionMessage;
        }

        public GetMessageHubsRequest GetExternalMessage(ConnectionMessage connectionMessage)
        {
            var externalMessage = new GetMessageHubsRequest()
            {
                Id = connectionMessage.Id,
                SecurityKey = connectionMessage.Parameters.First(p => p.Name == "SecurityKey").Value,
                //SenderAgentId = connectionMessage.Parameters.First(p => p.Name == "SenderAgentId").Value
            };

            return externalMessage;
        }
    }
}
