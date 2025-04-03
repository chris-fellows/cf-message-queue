using CFConnectionMessaging.Models;
using CFMessageQueue.Constants;
using CFMessageQueue.Exceptions;
using CFMessageQueue.Interfaces;
using CFMessageQueue.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace CFMessageQueue.Services
{
    public class MessageQueueClientConnector : IMessageQueueClientConnector, IDisposable
    {
        private MessageHubConnection _messageHubConnection = new MessageHubConnection();

        private MessageQueue? _messageQueue;

        private readonly string _securityKey;

        private int _localPort;

        private Action<string>? _notificationAction;

        public MessageQueueClientConnector(string securityKey, int localPort)
        {
            _securityKey = securityKey;
            _localPort = localPort;
        }

        public void Dispose()
        {
            _messageHubConnection.Dispose();            
        }

        public void SetMessageQueue(MessageQueue messageQueue)
        {
            // Clean up
            _messageHubConnection.StopListening();

            _messageQueue = messageQueue;
            _messageHubConnection = new MessageHubConnection();
            _messageHubConnection.OnConnectionMessageReceived += delegate (ConnectionMessage connectionMessage, MessageReceivedInfo messageReceivedInfo)
            {
                // If notification then forward
                if (connectionMessage.TypeId == MessageTypeIds.MessageQueueNotification &&
                    _notificationAction != null)
                {
                    var notification = _messageHubConnection.MessageConverterList.MessageQueueNotificationConverter.GetExternalMessage(connectionMessage);

                    // Notify
                    Task.Factory.StartNew(() => _notificationAction(notification.EventName));
                }
            };
          
            _messageHubConnection.StartListening(_localPort);
        }

        /// <summary>
        /// Gets queue message in internal format (Serialized content)
        /// </summary>
        /// <param name="queueMessage"></param>
        /// <returns></returns>
        private static QueueMessageInternal GetQueueMessageInternal(QueueMessage queueMessage)
        {
            var serializer = new QueueMessageContentSerializer();

            var queueMessageInternal = new QueueMessageInternal()
            {
                Id = queueMessage.Id,                
                Content = serializer.Serialize(queueMessage.Content, queueMessage.Content.GetType()),
                ContentType = queueMessage.Content.GetType().AssemblyQualifiedName,
                CreatedDateTime = queueMessage.CreatedDateTime,
                ExpirySeconds = queueMessage.ExpirySeconds,
                MessageQueueId = queueMessage.MessageQueueId,
                SenderMessageHubClientId = queueMessage.SenderMessageHubClientId,
                TypeId = queueMessage.TypeId                 
            };

            return queueMessageInternal;
        }

        /// <summary>
        /// Gets queue message in external format (Deserialized content)
        /// </summary>
        /// <param name="queueMessageInternal"></param>
        /// <returns></returns>
        private static QueueMessage GetQueueMessageExternal(QueueMessageInternal queueMessageInternal)
        {
            var serializer = new QueueMessageContentSerializer();

            var queueMessage = new QueueMessage()
            {
                Id = queueMessageInternal.Id,
                Content = serializer.Deserialize(queueMessageInternal.Content, Type.GetType(queueMessageInternal.ContentType)),
                CreatedDateTime = queueMessageInternal.CreatedDateTime,
                ExpirySeconds = queueMessageInternal.ExpirySeconds,
                MessageQueueId = queueMessageInternal.MessageQueueId,
                SenderMessageHubClientId = queueMessageInternal.SenderMessageHubClientId,
                TypeId = queueMessageInternal.TypeId
            };

            return queueMessage;
        }

        public async Task SendAsync(QueueMessage queueMessage)
        {
            var addQueueMessageRequest = new AddQueueMessageRequest()
            {
                SecurityKey = _securityKey,
                QueueMessage = GetQueueMessageInternal(queueMessage),
                MessageQueueId = _messageQueue.Id                
            };

            var remoteEndpointInfo = new EndpointInfo()
            {
                Ip = _messageQueue.Ip,
                Port = _messageQueue.Port
            };

            try
            {
                var response = _messageHubConnection.SendAddQueueMessageMessage(addQueueMessageRequest, remoteEndpointInfo);
                ThrowResponseExceptionIfRequired(response);
            }
            catch(MessageConnectionException messageConnectionException)
            {
                throw new MessageQueueException("Error adding message to queue", messageConnectionException);
            }
        }

        public async Task<QueueMessage?> GetNextAsync()
        {
            var getNextQueueMessageRequest = new GetNextQueueMessageRequest()
            {
                SecurityKey = _securityKey,
                MessageQueueId = _messageQueue.Id
            };

            var remoteEndpointInfo = new EndpointInfo()
            {
                Ip = _messageQueue.Ip,
                Port = _messageQueue.Port
            };

            try
            {
                var response = _messageHubConnection.SendGetNextQueueMessageRequest(getNextQueueMessageRequest, remoteEndpointInfo);
                ThrowResponseExceptionIfRequired(response);
                
                return response.QueueMessage == null ? null : GetQueueMessageExternal(response.QueueMessage);
            }
            catch (MessageConnectionException messageConnectionException)
            {
                throw new MessageQueueException("Error getting next queue message", messageConnectionException);
            }
        }

        public async Task<string?> SubscribeAsync(Action<string> notificationAction, TimeSpan queueSizeNotificationFrequency)
        {
            var messageQueueSubscribeRequest = new MessageQueueSubscribeRequest()
            {
                SecurityKey = _securityKey,
                MessageQueueId = _messageQueue.Id,
                ActionName = "SUBSCRIBE",
                QueueSizeFrequencySecs = Convert.ToInt64(queueSizeNotificationFrequency.TotalSeconds)
            };

            var remoteEndpointInfo = new EndpointInfo()
            {
                Ip = _messageQueue.Ip,
                Port = _messageQueue.Port
            };

            try
            {
                var response = _messageHubConnection.SendMessageQueueSubscribeRequest(messageQueueSubscribeRequest, remoteEndpointInfo);
                ThrowResponseExceptionIfRequired(response);

                // Set notification action
                _notificationAction = notificationAction;

                return response.SubscribeId;
            }
            catch (MessageConnectionException messageConnectionException)
            {
                throw new MessageQueueException("Error subscribing to message queue", messageConnectionException);
            }
        }

        public Task UnsubscribeAsync()
        {
            var messageQueueSubscribeRequest = new MessageQueueSubscribeRequest()
            {
                SecurityKey = _securityKey,
                MessageQueueId = _messageQueue.Id,
                ActionName = "UNSUBSCRIBE"
            };

            var remoteEndpointInfo = new EndpointInfo()
            {
                Ip = _messageQueue.Ip,
                Port = _messageQueue.Port
            };

            try
            {
                var response = _messageHubConnection.SendMessageQueueSubscribeRequest(messageQueueSubscribeRequest, remoteEndpointInfo);
                ThrowResponseExceptionIfRequired(response);

                // Clear subscribe action
                _notificationAction = null;
            }
            catch (MessageConnectionException messageConnectionException)
            {
                throw new MessageQueueException("Error subscribing to message queue", messageConnectionException);
            }

            return Task.CompletedTask;               
        }

        /// <summary>
        /// Checks connection message response and throws an exception if an error
        /// </summary>
        /// <param name="message"></param>
        /// <exception cref="MessageConnectionException"></exception>
        private void ThrowResponseExceptionIfRequired(MessageBase message)
        {
            if (message.Response != null && message.Response.ErrorCode != null)
            {
                throw new MessageConnectionException(message.Response.ErrorMessage);
            }
        }
    }
}
