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
    public class GetMessageHubClientsResponseConverter : IExternalMessageConverter<GetMessageHubClientsResponse>
    {
        public ConnectionMessage GetConnectionMessage(GetMessageHubClientsResponse externalMessage)
        {
            var connectionMessage = new ConnectionMessage()
            {
                Id = externalMessage.Id,
                TypeId = externalMessage.TypeId,
                Parameters = new List<ConnectionMessageParameter>()
                {
                   new ConnectionMessageParameter()
                    {
                        Name = "Response",
                        Value = externalMessage.Response == null ? "" :
                                    JsonUtilities.SerializeToBase64String(externalMessage.Response,
                                            JsonUtilities.DefaultJsonSerializerOptions)
                    },
                   new ConnectionMessageParameter()
                   {
                       Name = "MessageHubClients",
                       Value = externalMessage.MessageHubClients == null ? "" :
                                        JsonUtilities.SerializeToBase64String(externalMessage.MessageHubClients,
                                        JsonUtilities.DefaultJsonSerializerOptions)
                   }
                }
            };
            return connectionMessage;
        }

        public GetMessageHubClientsResponse GetExternalMessage(ConnectionMessage connectionMessage)
        {
            var externalMessage = new GetMessageHubClientsResponse()
            {
                Id = connectionMessage.Id,
            };

            // Get response
            var responseParameter = connectionMessage.Parameters.First(p => p.Name == "Response");
            if (!String.IsNullOrEmpty(responseParameter.Value))
            {
                externalMessage.Response = JsonUtilities.DeserializeFromBase64String<MessageResponse>(responseParameter.Value, JsonUtilities.DefaultJsonSerializerOptions);
            }

            // Get message hub clients
            var messageHubClientsParameter = connectionMessage.Parameters.First(p => p.Name == "MessageHubClients");
            if (!String.IsNullOrEmpty(messageHubClientsParameter.Value))
            {
                externalMessage.MessageHubClients = JsonUtilities.DeserializeFromBase64String<List<MessageHubClient>>(messageHubClientsParameter.Value, JsonUtilities.DefaultJsonSerializerOptions);
            }

            return externalMessage;
        }
    }
}
