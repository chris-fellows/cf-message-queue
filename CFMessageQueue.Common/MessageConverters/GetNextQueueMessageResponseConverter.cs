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
    public class GetNextQueueMessageResponseConverter : IExternalMessageConverter<GetNextQueueMessageResponse>
    {
        public ConnectionMessage GetConnectionMessage(GetNextQueueMessageResponse externalMessage)
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
                       Name = "QueueMessage",
                       Value = externalMessage.QueueMessage == null ? "" :
                                        JsonUtilities.SerializeToBase64String(externalMessage.QueueMessage,
                                        JsonUtilities.DefaultJsonSerializerOptions)
                   }
                }
            };
            return connectionMessage;
        }

        public GetNextQueueMessageResponse GetExternalMessage(ConnectionMessage connectionMessage)
        {
            var externalMessage = new GetNextQueueMessageResponse()
            {
                Id = connectionMessage.Id,
            };

            // Get response
            var responseParameter = connectionMessage.Parameters.First(p => p.Name == "Response");
            if (!String.IsNullOrEmpty(responseParameter.Value))
            {
                externalMessage.Response = JsonUtilities.DeserializeFromBase64String<MessageResponse>(responseParameter.Value, JsonUtilities.DefaultJsonSerializerOptions);
            }

            // Get queue message
            var queueMessageParameter = connectionMessage.Parameters.First(p => p.Name == "QueueMessage");
            if (!String.IsNullOrEmpty(queueMessageParameter.Value))
            {
                externalMessage.QueueMessage = JsonUtilities.DeserializeFromBase64String<QueueMessage>(queueMessageParameter.Value, JsonUtilities.DefaultJsonSerializerOptions);
            }

            return externalMessage;
        }
    }
}
