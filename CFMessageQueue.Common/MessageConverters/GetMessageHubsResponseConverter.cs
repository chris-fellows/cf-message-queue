using CFConnectionMessaging.Interfaces;
using CFConnectionMessaging.Models;
using CFMessageQueue.Constants;
using CFMessageQueue.Models;
using CFMessageQueue.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CFMessageQueue.MessageConverters
{
    public class GetMessageHubsResponseConverter : IExternalMessageConverter<GetMessageHubsResponse>
    {
        public ConnectionMessage GetConnectionMessage(GetMessageHubsResponse externalMessage)
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
                       Name = "MessageHubs",
                       Value = externalMessage.MessageHubs == null ? "" :
                                        JsonUtilities.SerializeToBase64String(externalMessage.MessageHubs,
                                        JsonUtilities.DefaultJsonSerializerOptions)
                   }
                }
            };
            return connectionMessage;
        }

        public GetMessageHubsResponse GetExternalMessage(ConnectionMessage connectionMessage)
        {
            var externalMessage = new GetMessageHubsResponse()
            {
                Id = connectionMessage.Id,
            };

            // Get response
            var responseParameter = connectionMessage.Parameters.First(p => p.Name == "Response");
            if (!String.IsNullOrEmpty(responseParameter.Value))
            {
                externalMessage.Response = JsonUtilities.DeserializeFromBase64String<MessageResponse>(responseParameter.Value, JsonUtilities.DefaultJsonSerializerOptions);
            }

            // Get message hubs
            var messageHubsParameter = connectionMessage.Parameters.First(p => p.Name == "MessageHubs");
            if (!String.IsNullOrEmpty(messageHubsParameter.Value))
            {
                externalMessage.MessageHubs = JsonUtilities.DeserializeFromBase64String<List<QueueMessageHub>>(messageHubsParameter.Value, JsonUtilities.DefaultJsonSerializerOptions);
            }

            return externalMessage;
        }
    }
}
