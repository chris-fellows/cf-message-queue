using CFConnectionMessaging.Models;
using CFMessageQueue.Constants;
using CFMessageQueue.Exceptions;
using CFMessageQueue.Interfaces;
using CFMessageQueue.Models;
using CFMessageQueue.Utilities;
using System.Threading.Channels;

namespace CFMessageQueue.Services
{
    /// <summary>
    /// Message queue client connector. Communicates with queue worker for specific queue.
    /// 
    /// Note that we don't need to validate most parameters because the Hub validates them and it just means that there's
    /// duplicate validation logic.
    /// </summary>
    public class MessageQueueClientConnector : ClientConnectorBase, IMessageQueueClientConnector, IDisposable
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
                    _messageHubConnection.OnMessageReceived += delegate (MessageBase messageBase, MessageReceivedInfo messageReceivedInfo)
                    {
                        // If response then forward to relevant session
                        if (messageBase.Response != null &&
                                _responseSessions.ContainsKey(messageBase.Response.MessageId))
                        {
                            var writer = _responseSessions[messageBase.Response.MessageId].MessagesChannel.Writer;
                            writer.WriteAsync(new Tuple<MessageBase, MessageReceivedInfo>(messageBase, messageReceivedInfo));
                        }                        
                        else if (messageBase.TypeId == MessageTypeIds.MessageQueueNotification &&
                                    _notificationAction != null)
                        {                            
                            var notification = (MessageQueueNotificationMessage)messageBase;

                            // Notify
                            Task.Factory.StartNew(() => _notificationAction(notification.EventName, notification.QueueSize));                            
                        }                        
                    };

                    _messageHubConnection.StartListening(_localPort);
                }
            }
        }

        private static NewQueueMessageInternal GetNewQueueMessageInternal(NewQueueMessage queueMessage)
        {
            var serializer = new QueueMessageContentSerializer();

            var queueMessageInternal = new NewQueueMessageInternal()
            {                
                Content = serializer.Serialize(queueMessage.Content, queueMessage.Content.GetType()),
                ContentType = queueMessage.Content.GetType().AssemblyQualifiedName,                
                ExpirySeconds = queueMessage.ExpirySeconds,                
                Name = queueMessage.Name,
                Priority = queueMessage.Priority,
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
                Name = queueMessageInternal.Name,
                Priority = queueMessageInternal.Priority,
                SenderMessageHubClientId = queueMessageInternal.SenderMessageHubClientId,
                TypeId = queueMessageInternal.TypeId
            };

            return queueMessage;
        }

        public async Task<bool> SendAsync(NewQueueMessage queueMessage)
        {
            if (_messageQueue == null)
            {
                throw new ArgumentException("Queue must be set");
            }

            return await Task.Run(async () =>
            {
                using (var disposableSession = new DisposableSession())
                {
                    var request = new AddQueueMessageRequest()
                    {
                        SecurityKey = _securityKey,
                        ClientSessionId = _clientSessionId,
                        QueueMessage = GetNewQueueMessageInternal(queueMessage),
                        MessageQueueId = _messageQueue.Id
                    };

                    var remoteEndpointInfo = new EndpointInfo()
                    {
                        Ip = _messageQueue.Ip,
                        Port = _messageQueue.Port
                    };

                    try
                    {
                        var responsesSession = new MessageReceiveSession(request.Id, new CancellationTokenSource());
                        _responseSessions.Add(responsesSession.MessageId, responsesSession);
                        disposableSession.Add(() =>
                        {
                            lock (_responseSessions)
                            {
                                if (_responseSessions.ContainsKey(responsesSession.MessageId)) _responseSessions.Remove(responsesSession.MessageId);
                            }
                        });

                        _messageHubConnection.SendMessage(request, remoteEndpointInfo);

                        // Wait for response                        
                        responsesSession.CancellationTokenSource.CancelAfter(_responseTimeout);
                        var responseMessages = await WaitForResponsesAsync(request, responsesSession);

                        // Check response
                        ThrowResponseExceptionIfRequired(responseMessages.FirstOrDefault());                        
                    }
                    catch (MessageConnectionException messageConnectionException)
                    {
                        throw new MessageQueueException("Error adding message to queue", messageConnectionException);
                    }

                    return true;
                }
            });
        }

        public async Task<QueueMessage?> GetNextAsync(TimeSpan maxWait, TimeSpan maxProcessingTime)
        {
            if (_messageQueue == null)
            {
                throw new ArgumentException("Queue must be set");
            }

            return await Task.Run(async () =>
            {
                using (var disposableSession = new DisposableSession())
                {
                    var request = new GetNextQueueMessageRequest()
                    {
                        SecurityKey = _securityKey,
                        ClientSessionId = _clientSessionId,
                        MessageQueueId = _messageQueue.Id,
                        MaxWaitMilliseconds = (int)maxWait.TotalMilliseconds,
                        MaxProcessingSeconds = (int)maxProcessingTime.TotalSeconds
                    };

                    var remoteEndpointInfo = new EndpointInfo()
                    {
                        Ip = _messageQueue.Ip,
                        Port = _messageQueue.Port
                    };

                    try
                    {
                        var responsesSession = new MessageReceiveSession(request.Id, new CancellationTokenSource());
                        _responseSessions.Add(responsesSession.MessageId, responsesSession);
                        disposableSession.Add(() =>
                        {
                            lock (_responseSessions)
                            {
                                if (_responseSessions.ContainsKey(responsesSession.MessageId)) _responseSessions.Remove(responsesSession.MessageId);
                            }
                        });

                        _messageHubConnection.SendMessage(request, remoteEndpointInfo);

                        // Wait for response                        
                        responsesSession.CancellationTokenSource.CancelAfter(_responseTimeout);
                        var responseMessages = await WaitForResponsesAsync(request, responsesSession);

                        // Check response
                        ThrowResponseExceptionIfRequired(responseMessages.FirstOrDefault());

                        var response = (GetNextQueueMessageResponse)responseMessages.First();

                        return response.QueueMessage == null ? null : GetQueueMessageExternal(response.QueueMessage);
                    }
                    catch (MessageConnectionException messageConnectionException)
                    {
                        throw new MessageQueueException("Error getting next queue message", messageConnectionException);
                    }
                }
            });
        }
        
        public async Task<bool> SetProcessed(string queueMessageId, bool processed)
        {
            if (String.IsNullOrEmpty(queueMessageId))
            {
                throw new ArgumentNullException(nameof(queueMessageId));
            }
            if (_messageQueue == null)
            {
                throw new ArgumentException("Queue must be set");
            }

            return await Task.Run(async () =>
            {
                using (var disposableSession = new DisposableSession())
                {
                    var request = new QueueMessageProcessedRequest()
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
                        var responsesSession = new MessageReceiveSession(request.Id, new CancellationTokenSource());
                        _responseSessions.Add(responsesSession.MessageId, responsesSession);
                        disposableSession.Add(() =>
                        {
                            lock (_responseSessions)
                            {
                                if (_responseSessions.ContainsKey(responsesSession.MessageId)) _responseSessions.Remove(responsesSession.MessageId);
                            }
                        });

                        _messageHubConnection.SendMessage(request, remoteEndpointInfo);

                        // Wait for response                        
                        responsesSession.CancellationTokenSource.CancelAfter(_responseTimeout);
                        var responseMessages = await WaitForResponsesAsync(request, responsesSession);

                        // Check response
                        ThrowResponseExceptionIfRequired(responseMessages.FirstOrDefault());
                    }
                    catch (MessageConnectionException messageConnectionException)
                    {
                        throw new MessageQueueException("Error notifying that queue message was processed", messageConnectionException);
                    }

                    return true;
                }
            });
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

            return await Task.Run(async () =>
            {
                using (var disposableSession = new DisposableSession())
                {
                    var request = new GetQueueMessagesRequest()
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
                        var responsesSession = new MessageReceiveSession(request.Id, new CancellationTokenSource());
                        _responseSessions.Add(responsesSession.MessageId, responsesSession);
                        disposableSession.Add(() =>
                        {
                            lock (_responseSessions)
                            {
                                if (_responseSessions.ContainsKey(responsesSession.MessageId)) _responseSessions.Remove(responsesSession.MessageId);
                            }
                        });

                        _messageHubConnection.SendMessage(request, remoteEndpointInfo);

                        // Wait for response                        
                        responsesSession.CancellationTokenSource.CancelAfter(_responseTimeout);
                        var responseMessages = await WaitForResponsesAsync(request, responsesSession);

                        // Check response
                        ThrowResponseExceptionIfRequired(responseMessages.FirstOrDefault());

                        var response = (GetQueueMessagesResponse)responseMessages.First();

                        return response.QueueMessages == null ? new() : response.QueueMessages.Select(m => GetQueueMessageExternal(m)).ToList();
                    }
                    catch (MessageConnectionException messageConnectionException)
                    {
                        throw new MessageQueueException("Error getting queue messages", messageConnectionException);
                    }
                }
            });
        }

        public async Task<string?> SubscribeAsync(Action<string, long?> notificationAction, TimeSpan queueSizeNotificationFrequency)
        {
            if (_messageQueue == null)
            {
                throw new ArgumentException("Queue must be set");
            }

            return await Task.Run(async () =>
            {
                using (var disposableSession = new DisposableSession())
                {
                    var request = new MessageQueueSubscribeRequest()
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
                        var responsesSession = new MessageReceiveSession(request.Id, new CancellationTokenSource());
                        _responseSessions.Add(responsesSession.MessageId, responsesSession);
                        disposableSession.Add(() =>
                        {
                            lock (_responseSessions)
                            {
                                if (_responseSessions.ContainsKey(responsesSession.MessageId)) _responseSessions.Remove(responsesSession.MessageId);
                            }
                        });

                        _messageHubConnection.SendMessage(request, remoteEndpointInfo);

                        // Wait for response                        
                        responsesSession.CancellationTokenSource.CancelAfter(_responseTimeout);
                        var responseMessages = await WaitForResponsesAsync(request, responsesSession);

                        // Check response
                        ThrowResponseExceptionIfRequired(responseMessages.FirstOrDefault());

                        var response = (MessageQueueSubscribeResponse)responseMessages.First();
                        
                        // Set notification action
                        _notificationAction = notificationAction;

                        return response.SubscribeId;
                    }
                    catch (MessageConnectionException messageConnectionException)
                    {
                        throw new MessageQueueException("Error subscribing to message queue", messageConnectionException);
                    }
                }
            });
        }

        public async Task<bool> UnsubscribeAsync()
        {
            if (_messageQueue == null)
            {
                throw new ArgumentException("Queue must be set");
            }

            return await Task.Run(async () =>
            {
                using (var disposableSession = new DisposableSession())
                {
                    var request = new MessageQueueSubscribeRequest()
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
                        var responsesSession = new MessageReceiveSession(request.Id, new CancellationTokenSource());
                        _responseSessions.Add(responsesSession.MessageId, responsesSession);
                        disposableSession.Add(() =>
                        {
                            lock (_responseSessions)
                            {
                                if (_responseSessions.ContainsKey(responsesSession.MessageId)) _responseSessions.Remove(responsesSession.MessageId);
                            }
                        });

                        _messageHubConnection.SendMessage(request, remoteEndpointInfo);

                        // Wait for response                        
                        responsesSession.CancellationTokenSource.CancelAfter(_responseTimeout);
                        var responseMessages = await WaitForResponsesAsync(request, responsesSession);

                        // Check response
                        ThrowResponseExceptionIfRequired(responseMessages.FirstOrDefault());

                        // Clear subscribe action
                        _notificationAction = null;
                    }
                    catch (MessageConnectionException messageConnectionException)
                    {
                        throw new MessageQueueException("Error subscribing to message queue", messageConnectionException);
                    }

                    return true;
                }
            });               
        }     
    }
}
