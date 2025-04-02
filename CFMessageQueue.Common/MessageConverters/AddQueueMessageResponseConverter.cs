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
    public class AddQueueMessageResponseConverter : IExternalMessageConverter<AddQueueMessageResponse>
    {
        public ConnectionMessage GetConnectionMessage(AddQueueMessageResponse externalMessage)
        {
            var connectionMessage = new ConnectionMessage()
            {
                Id = externalMessage.Id,
                TypeId = externalMessage.TypeId,
                Parameters = new List<ConnectionMessageParameter>()
                {
                    new ConnectionMessageParameter()
                    {
                        Name = "Ip",
                        Value = externalMessage.Ip
                    },
                    new ConnectionMessageParameter()
                    {
                        Name = "Port",
                        Value = externalMessage.Port.ToString()
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

        public AddQueueMessageResponse GetExternalMessage(ConnectionMessage connectionMessage)
        {
            var externalMessage = new AddQueueMessageResponse()
            {
                Id = connectionMessage.Id,
                Ip = connectionMessage.Parameters.First(p => p.Name == "Ip").Value,
                Port = Convert.ToInt32(connectionMessage.Parameters.First(p => p.Name == "Port").Value)
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
