using CFConnectionMessaging.Models;
using CFMessageQueue.Constants;
using CFMessageQueue.Exceptions;
using CFMessageQueue.Interfaces;
using CFMessageQueue.Models;

namespace CFMessageQueue.Services
{
    /// <summary>
    /// Message queue client connector. Communicates with queue worker for specific queue.
    /// </summary>
    public class MessageQueueClientConnector : IMessageQueueClientConnector, IDisposable
    {
        private MessageHubConnection _messageHubConnection = new MessageHubConnection();

        private MessageQueue? _messageQueue;

        private readonly string _clientSessionId = Guid.NewGuid().ToString();
        private readonly string _securityKey;

        private int _localPort;

        private Action<string, long?>? _notificationAction;      // Params: Event Name, Queue Size

        public MessageQueueClientConnector(string securityKey, int localPort)
        {
            _securityKey = securityKey;
            _localPort = localPort;
        }

        public void Dispose()
        {
            _messageHubConnection.Dispose();            
        }

        public MessageQueue? MessageQueue
        {
            get { return _messageQueue; }
            set
            {
                // Clean up
                _messageHubConnection.StopListening();

                _messageQueue = value;
                if (_messageQueue != null)
                {
                    _messageHubConnection = new MessageHubConnection();
                    _messageHubConnection.OnConnectionMessageReceived += delegate (ConnectionMessage connectionMessage, MessageReceivedInfo messageReceivedInfo)
                    {
                        // If notification then forward it
                        if (connectionMessage.TypeId == MessageTypeIds.MessageQueueNotification &&
                            _notificationAction != null)
                        {
                            var notification = _messageHubConnection.MessageConverterList.MessageQueueNotificationMessageConverter.GetExternalMessage(connectionMessage);

                            // Notify
                            Task.Factory.StartNew(() => _notificationAction(notification.EventName, notification.QueueSize));
                        }
                    };

                    _messageHubConnection.StartListening(_localPort);
                }
            }
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
            if (_messageQueue == null)
            {
                throw new ArgumentException("Queue must be set");
            }

            var addQueueMessageRequest = new AddQueueMessageRequest()
            {
                SecurityKey = _securityKey,
                ClientSessionId = _clientSessionId,
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

        public async Task<QueueMessage?> GetNextAsync(TimeSpan maxWait)
        {
            if (_messageQueue == null)
            {
                throw new ArgumentException("Queue must be set");
            }

            var getNextQueueMessageRequest = new GetNextQueueMessageRequest()
            {
                SecurityKey = _securityKey,
                ClientSessionId = _clientSessionId,
                MessageQueueId = _messageQueue.Id,
                MaxWaitMilliseconds = (int)maxWait.TotalMilliseconds
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
        
        public async Task SetProcessed(string queueMessageId, bool processed)
        {
            if (String.IsNullOrEmpty(queueMessageId))
            {
                throw new ArgumentNullException(nameof(queueMessageId));
            }
            if (_messageQueue == null)
            {
                throw new ArgumentException("Queue must be set");
            }

            var queueMessageProcessedMessage = new QueueMessageProcessedMessage()
            {
                SecurityKey = _securityKey,
                ClientSessionId = _clientSessionId,
                MessageQueueId = _messageQueue.Id,
                QueueMessageId = queueMessageId,
                Processed = processed
            };

            var remoteEndpointInfo = new EndpointInfo()
            {
                Ip = _messageQueue.Ip,
                Port = _messageQueue.Port
            };

            try
            {
                _messageHubConnection.SendQueueMessageProcessedMessage(queueMessageProcessedMessage, remoteEndpointInfo);                                
            }
            catch (MessageConnectionException messageConnectionException)
            {
                throw new MessageQueueException("Error notifying that queue message was processed", messageConnectionException);
            }        
        }

        public async Task<List<QueueMessage>> GetQueueMessages(int pageItems, int page)
        {
            if (_messageQueue == null)
            {
                throw new ArgumentException("Queue must be set");
            }
            if (pageItems < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(pageItems), "Page Items must be 1 or more");
            }
            if (page < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(page), "Page must be 1 or more");
            }

            var getQueueMessagesRequest = new GetQueueMessagesRequest()
            {
                SecurityKey = _securityKey,
                ClientSessionId = _clientSessionId,
                MessageQueueId = _messageQueue.Id,
                PageItems = pageItems,
                Page = page
            };

            var remoteEndpointInfo = new EndpointInfo()
            {
                Ip = _messageQueue.Ip,
                Port = _messageQueue.Port
            };

            try
            {
                var response = _messageHubConnection.SendGetQueueMessagesRequest(getQueueMessagesRequest, remoteEndpointInfo);
                ThrowResponseExceptionIfRequired(response);

                return response.QueueMessages == null ? null : response.QueueMessages.Select(m => GetQueueMessageExternal(m)).ToList();
            }
            catch (MessageConnectionException messageConnectionException)
            {
                throw new MessageQueueException("Error getting queue messages", messageConnectionException);
            }
        }

        public async Task<string?> SubscribeAsync(Action<string, long?> notificationAction, TimeSpan queueSizeNotificationFrequency)
        {
            if (_messageQueue == null)
            {
                throw new ArgumentException("Queue must be set");
            }

            var messageQueueSubscribeRequest = new MessageQueueSubscribeRequest()
            {
                SecurityKey = _securityKey,
                ClientSessionId = _clientSessionId,
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
            if (_messageQueue == null)
            {
                throw new ArgumentException("Queue must be set");
            }

            var messageQueueSubscribeRequest = new MessageQueueSubscribeRequest()
            {
                SecurityKey = _securityKey,
                ClientSessionId = _clientSessionId,
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
