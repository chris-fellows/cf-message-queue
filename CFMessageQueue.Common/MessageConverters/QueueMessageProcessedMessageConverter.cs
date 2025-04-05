using CFConnectionMessaging.Interfaces;
using CFConnectionMessaging.Models;
using CFMessageQueue.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CFMessageQueue.MessageConverters
{
    public class QueueMessageProcessedMessageConverter : IExternalMessageConverter<QueueMessageProcessedMessage>
    {
        public ConnectionMessage GetConnectionMessage(QueueMessageProcessedMessage externalMessage)
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
                        Name = "QueueMessageId",
                        Value = externalMessage.QueueMessageId
                    },
                    new ConnectionMessageParameter()
                    {
                        Name = "Processed",
                        Value = externalMessage.Processed.ToString()
                    }

                }
            };
            return connectionMessage;
        }

        public QueueMessageProcessedMessage GetExternalMessage(ConnectionMessage connectionMessage)
        {
            var externalMessage = new QueueMessageProcessedMessage()
            {
                Id = connectionMessage.Id,
                SecurityKey = connectionMessage.Parameters.First(p => p.Name == "SecurityKey").Value,
                ClientSessionId = connectionMessage.Parameters.First(p => p.Name == "ClientSessionId").Value,
                MessageQueueId = connectionMessage.Parameters.First(p => p.Name == "MessageQueueId").Value,
                QueueMessageId = connectionMessage.Parameters.First(p => p.Name == "QueueMessageId").Value,
                Processed = Convert.ToBoolean(connectionMessage.Parameters.First(p => p.Name == "Processed").Value)
            };

            return externalMessage;
        }
    }
}
