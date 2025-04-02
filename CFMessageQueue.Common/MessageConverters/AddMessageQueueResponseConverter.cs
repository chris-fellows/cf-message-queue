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
    public class AddMessageQueueResponseConverter : IExternalMessageConverter<AddMessageQueueResponse>
    {
        public ConnectionMessage GetConnectionMessage(AddMessageQueueResponse externalMessage)
        {
            var connectionMessage = new ConnectionMessage()
            {
                Id = externalMessage.Id,
                TypeId = externalMessage.TypeId,
                Parameters = new List<ConnectionMessageParameter>()
                {
                    new ConnectionMessageParameter()
                    {
                        Name = "MessageQueueId",
                        Value = externalMessage.MessageQueueId
                    },
                   new ConnectionMessageParameter()
                    {
                        Name = "Response",
                        Value = externalMessage.Response == null ? "" :
                                    JsonUtilities.SerializeToBase64String(externalMessage.Response,
                                            JsonUtilities.DefaultJsonSerializerOptions)
                    }
                }
            };
            return connectionMessage;
        }

        public AddMessageQueueResponse GetExternalMessage(ConnectionMessage connectionMessage)
        {
            var externalMessage = new AddMessageQueueResponse()
            {
                Id = connectionMessage.Id,
                MessageQueueId = connectionMessage.Parameters.First(p => p.Name == "MessageQueueId").Value
            };

            // Get response
            var responseParameter = connectionMessage.Parameters.First(p => p.Name == "Response");
            if (!String.IsNullOrEmpty(responseParameter.Value))
            {
                externalMessage.Response = JsonUtilities.DeserializeFromBase64String<MessageResponse>(responseParameter.Value, JsonUtilities.DefaultJsonSerializerOptions);
            }

            return externalMessage;
        }
    }
}
