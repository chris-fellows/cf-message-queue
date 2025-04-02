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
    public class GetMessageQueuesResponseConverter : IExternalMessageConverter<GetMessageQueuesResponse>
    {
        public ConnectionMessage GetConnectionMessage(GetMessageQueuesResponse externalMessage)
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
                       Name = "MessageQueues",
                       Value = externalMessage.MessageQueues == null ? "" :
                                        JsonUtilities.SerializeToBase64String(externalMessage.MessageQueues,
                                        JsonUtilities.DefaultJsonSerializerOptions)
                   }
                }
            };
            return connectionMessage;
        }

        public GetMessageQueuesResponse GetExternalMessage(ConnectionMessage connectionMessage)
        {
            var externalMessage = new GetMessageQueuesResponse()
            {
                Id = connectionMessage.Id,
            };

            // Get response
            var responseParameter = connectionMessage.Parameters.First(p => p.Name == "Response");
            if (!String.IsNullOrEmpty(responseParameter.Value))
            {
                externalMessage.Response = JsonUtilities.DeserializeFromBase64String<MessageResponse>(responseParameter.Value, JsonUtilities.DefaultJsonSerializerOptions);
            }

            // Get message queues
            var messageQueuesParameter = connectionMessage.Parameters.First(p => p.Name == "MessageQueues");
            if (!String.IsNullOrEmpty(messageQueuesParameter.Value))
            {
                externalMessage.MessageQueues = JsonUtilities.DeserializeFromBase64String<List<MessageQueue>>(messageQueuesParameter.Value, JsonUtilities.DefaultJsonSerializerOptions);
            }

            return externalMessage;
        }
    }
}
