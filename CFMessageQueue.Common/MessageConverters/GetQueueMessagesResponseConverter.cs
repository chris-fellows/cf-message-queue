using CFConnectionMessaging.Interfaces;
using CFConnectionMessaging.Models;
using CFMessageQueue.Models;
using CFMessageQueue.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace CFMessageQueue.MessageConverters
{
    public class GetQueueMessagesResponseConverter : IExternalMessageConverter<GetQueueMessagesResponse>
    {
        public ConnectionMessage GetConnectionMessage(GetQueueMessagesResponse externalMessage)
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
                       Name = "QueueMessages",
                       Value = externalMessage.QueueMessages == null ? "" :
                                        JsonUtilities.SerializeToBase64String(externalMessage.QueueMessages,
                                        JsonUtilities.DefaultJsonSerializerOptions)
                   }
                }
            };
            return connectionMessage;
        }

        public GetQueueMessagesResponse GetExternalMessage(ConnectionMessage connectionMessage)
        {
            var externalMessage = new GetQueueMessagesResponse()
            {
                Id = connectionMessage.Id,
            };

            // Get response
            var responseParameter = connectionMessage.Parameters.First(p => p.Name == "Response");
            if (!String.IsNullOrEmpty(responseParameter.Value))
            {
                externalMessage.Response = JsonUtilities.DeserializeFromBase64String<MessageResponse>(responseParameter.Value, JsonUtilities.DefaultJsonSerializerOptions);
            }

            // Get queue messages
            var queueMessagesParameter = connectionMessage.Parameters.First(p => p.Name == "QueueMessages");
            if (!String.IsNullOrEmpty(queueMessagesParameter.Value))
            {
                externalMessage.QueueMessages = JsonUtilities.DeserializeFromBase64String<List<QueueMessageInternal>>(queueMessagesParameter.Value, JsonUtilities.DefaultJsonSerializerOptions);
            }

            return externalMessage;
        }
    }
}
