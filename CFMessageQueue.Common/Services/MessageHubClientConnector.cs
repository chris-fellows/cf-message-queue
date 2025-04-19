using CFConnectionMessaging.Models;
using CFMessageQueue.Enums;
using CFMessageQueue.Exceptions;
using CFMessageQueue.Interfaces;
using CFMessageQueue.Models;
using CFMessageQueue.Utilities;
using System.Threading.Channels;

namespace CFMessageQueue.Services
{
    /// <summary>
    /// Message hub client connector. Communicates with hub worker.
    /// </summary>
    public class MessageHubClientConnector : ClientConnectorBase, IMessageHubClientConnector, IDisposable
    {
        private readonly MessageHubConnection _messageHubConnection = new MessageHubConnection();

        private readonly string _clientSessionId = Guid.NewGuid().ToString();
        private readonly EndpointInfo _remoteEndpointInfo;
        private readonly string _securityKey;        
      
        public MessageHubClientConnector(EndpointInfo remoteEndpointInfo, string securityKey, int localPort)
        {            
            _remoteEndpointInfo = remoteEndpointInfo;
            _securityKey = securityKey;

            // Set event handler to accumulate messages received
            _messageHubConnection.OnMessageReceived += delegate (MessageBase messageBase, MessageReceivedInfo messageReceivedInfo)
            {
                // If response then forward to relevant session
                if (messageBase.Response != null &&
                    _responseSessions.ContainsKey(messageBase.Response.MessageId))
                {
                    _responseSessions[messageBase.Response.MessageId].MessagesChannel.Writer.WriteAsync(new Tuple<MessageBase, MessageReceivedInfo>(messageBase, messageReceivedInfo));
                }
            };

            _messageHubConnection.StartListening(localPort);
        }

        public void Dispose()
        {
            // Cancel any active requests/responses
            foreach(var session in _responseSessions.Values)
            {
                if (!session.CancellationTokenSource.IsCancellationRequested)
                {
                    session.CancellationTokenSource.Cancel();
                }
            }

            _messageHubConnection.Dispose();
        }

        public string CreateRandomSecurityKey()
        {
            return Guid.NewGuid().ToString();
        }

        public async Task<bool> ConfigureMessageHubClientAsync(string messageHubClientId, List<RoleTypes> roleTypes)
        {            
            if (String.IsNullOrEmpty(messageHubClientId))
            {
                throw new ArgumentNullException(nameof(messageHubClientId));
            }           

            return await Task.Run(async () =>
            {
                using (var disposableSession = new DisposableSession())
                {
                    var request = new ConfigureMessageHubClientRequest()
                    {
                        SecurityKey = _securityKey,
                        ClientSessionId = _clientSessionId,
                        MessageHubClientId = messageHubClientId,
                        MessageQueueId = "",
                        RoleTypes = roleTypes     // Will return error if they specify non-hub roles
                    };

                    try
                    {
                        var responsesSession = new MessageReceiveSession(request.Id,new CancellationTokenSource());
                        _responseSessions.Add(responsesSession.MessageId, responsesSession);
                        disposableSession.Add(() =>
                        {
                            lock (_responseSessions)
                            {
                                if (_responseSessions.ContainsKey(responsesSession.MessageId)) _responseSessions.Remove(responsesSession.MessageId);
                            }
                        });

                        _messageHubConnection.SendMessage(request, _remoteEndpointInfo);

                        // Wait for response                        
                        responsesSession.CancellationTokenSource.CancelAfter(_responseTimeout);
                        var responseMessages = await WaitForResponsesAsync(request, responsesSession);

                        // Check response
                        ThrowResponseExceptionIfRequired(responseMessages.FirstOrDefault());
                    }
                    catch (MessageConnectionException messageConnectionException)
                    {
                        throw new MessageQueueException("Error configuring message hub client", messageConnectionException);
                    }
                }

                return true;
            });
        }

        public async Task<bool> ConfigureMessageHubClientAsync(string messageHubClientId, string messageQueueId, List<RoleTypes> roleTypes)
        {
            if (String.IsNullOrEmpty(messageHubClientId))
            {
                throw new ArgumentNullException(nameof(messageHubClientId));
            }
            if (String.IsNullOrEmpty(messageQueueId))
            {
                throw new ArgumentNullException(nameof(messageQueueId));
            }

            return await Task.Run(async () =>
            {
                using (var disposableSession = new DisposableSession())
                {
                    var request = new ConfigureMessageHubClientRequest()
                    {
                        SecurityKey = _securityKey,
                        ClientSessionId = _clientSessionId,
                        MessageHubClientId = messageHubClientId,
                        MessageQueueId = messageQueueId,
                        RoleTypes = roleTypes       // Will return error if they specify non-queue roles
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

                        _messageHubConnection.SendMessage(request, _remoteEndpointInfo);

                        // Wait for response                        
                        responsesSession.CancellationTokenSource.CancelAfter(_responseTimeout);
                        var responseMessages = await WaitForResponsesAsync(request, responsesSession);

                        // Check response
                        ThrowResponseExceptionIfRequired(responseMessages.FirstOrDefault());
                    }
                    catch (MessageConnectionException messageConnectionException)
                    {
                        throw new MessageQueueException("Error configuring message hub client", messageConnectionException);
                    }

                    return true;
                }
            });            
        }

        public async Task<string> AddMessageQueueAsync(string name, int maxConcurrentProcessing, int maxSize)
        {
            if (String.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name));
            }
            if (maxConcurrentProcessing < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxConcurrentProcessing), "Max Concurrent Processing must be zero or more");
            }
            if (maxSize < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxSize), "Max Size must be zero or more");
            }

            return await Task.Run(async () =>
            {
                using (var disposableSession = new DisposableSession())
                {
                    var request = new AddMessageQueueRequest()
                    {
                        SecurityKey = _securityKey,
                        ClientSessionId = _clientSessionId,
                        MessageQueueName = name,
                        MaxConcurrentProcessing = maxConcurrentProcessing,
                        MaxSize = maxSize
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

                        _messageHubConnection.SendMessage(request, _remoteEndpointInfo);

                        // Wait for response                        
                        responsesSession.CancellationTokenSource.CancelAfter(_responseTimeout);
                        var responseMessages = await WaitForResponsesAsync(request, responsesSession);

                        // Check response
                        ThrowResponseExceptionIfRequired(responseMessages.FirstOrDefault());

                        // Return response
                        var response = (AddMessageQueueResponse)responseMessages.First();
                        return response.MessageQueueId;
                    }
                    catch (MessageConnectionException messageConnectionException)
                    {
                        throw new MessageQueueException("Error adding message queue", messageConnectionException);
                    }
                }
            });
        }

        public async Task<bool> DeleteMessageQueueAsync(string messageQueueId)
        {
            if (String.IsNullOrEmpty(messageQueueId))
            {
                throw new ArgumentNullException(nameof(messageQueueId));
            }

            return await Task.Run(async () =>
            {
                using (var disposableSession = new DisposableSession())
                {
                    var request = new ExecuteMessageQueueActionRequest()
                    {
                        SecurityKey = _securityKey,
                        ClientSessionId = _clientSessionId,
                        MessageQueueId = messageQueueId,
                        ActionName = "DELETE"
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

                        _messageHubConnection.SendMessage(request, _remoteEndpointInfo);

                        // Wait for response
                        responsesSession.CancellationTokenSource.CancelAfter(_responseTimeout);
                        var responseMessages = await WaitForResponsesAsync(request, responsesSession);

                        // Check response
                        ThrowResponseExceptionIfRequired(responseMessages.FirstOrDefault());
                    }
                    catch (MessageConnectionException messageConnectionException)
                    {
                        throw new MessageQueueException("Error deleting message queue", messageConnectionException);
                    }

                    return true;
                }
            });
        }

        public async Task<bool> ClearMessageQueueAsync(string messageQueueId)
        {
            if (String.IsNullOrEmpty(messageQueueId))
            {
                throw new ArgumentNullException(nameof(messageQueueId));
            }

            return await Task.Run(async () =>
            {
                using (var disposableSession = new DisposableSession())
                {

                    var request = new ExecuteMessageQueueActionRequest()
                    {
                        SecurityKey = _securityKey,
                        ClientSessionId = _clientSessionId,
                        MessageQueueId = messageQueueId,
                        ActionName = "CLEAR"
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

                        _messageHubConnection.SendMessage(request, _remoteEndpointInfo);

                        // Wait for response                       
                        responsesSession.CancellationTokenSource.CancelAfter(_responseTimeout);
                        var responseMessages = await WaitForResponsesAsync(request, responsesSession);

                        // Check response
                        ThrowResponseExceptionIfRequired(responseMessages.FirstOrDefault());
                    }
                    catch (MessageConnectionException messageConnectionException)
                    {
                        throw new MessageQueueException("Error clearing message queue", messageConnectionException);
                    }

                    return true;
                }
            });
        }

        public async Task<string> AddMessageHubClientAsync(string name, string securityKey)
        {
            if (String.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name));
            }
            if (String.IsNullOrEmpty(securityKey))
            {
                throw new ArgumentNullException(nameof(securityKey));
            }

            return await Task.Run(async () =>
            {
                using (var disposableSession = new DisposableSession())
                {
                    var request = new AddMessageHubClientRequest()
                    {
                        SecurityKey = _securityKey,
                        ClientSessionId = _clientSessionId,
                        Name = name,
                        ClientSecurityKey = securityKey
                    };

                    try
                    {
                        var responsesSession = new MessageReceiveSession(request.Id,new CancellationTokenSource());
                        _responseSessions.Add(responsesSession.MessageId, responsesSession);
                        disposableSession.Add(() =>
                        {
                            lock (_responseSessions)
                            {
                                if (_responseSessions.ContainsKey(responsesSession.MessageId)) _responseSessions.Remove(responsesSession.MessageId);
                            }
                        });

                        _messageHubConnection.SendMessage(request, _remoteEndpointInfo);

                        // Wait for response                        
                        responsesSession.CancellationTokenSource.CancelAfter(_responseTimeout);
                        var responseMessages = await WaitForResponsesAsync(request, responsesSession);

                        // Check response
                        ThrowResponseExceptionIfRequired(responseMessages.FirstOrDefault());

                        var response = (AddMessageHubClientResponse)responseMessages.First();
                        return response.MessageHubClientId;
                    }
                    catch (MessageConnectionException messageConnectionException)
                    {
                        throw new MessageQueueException("Error getting message hubs", messageConnectionException);
                    }
                }
            });
        }

        public async Task<List<MessageHubClient>> GetMessageHubClientsAsync()
        {
            return await Task.Run(async () =>
            {
                using (var disposableSession = new DisposableSession())
                {
                    var request = new GetMessageHubClientsRequest()
                    {
                        SecurityKey = _securityKey,
                        ClientSessionId = _clientSessionId,
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

                        _messageHubConnection.SendMessage(request, _remoteEndpointInfo);

                        // Wait for response                        
                        responsesSession.CancellationTokenSource.CancelAfter(_responseTimeout);
                        var responseMessages = await WaitForResponsesAsync(request, responsesSession);

                        // Check response
                        ThrowResponseExceptionIfRequired(responseMessages.FirstOrDefault());

                        var response = (GetMessageHubClientsResponse)responseMessages.First();

                        return response.MessageHubClients;
                    }
                    catch (MessageConnectionException messageConnectionException)
                    {
                        throw new MessageQueueException("Error getting message hub clients", messageConnectionException);
                    }
                }
            });
        }

        public async Task<List<QueueMessageHub>> GetMessageHubsAsync()
        {
            return await Task.Run(async () =>
            {
                using (var disposableSession = new DisposableSession())
                {
                    var request = new GetMessageHubsRequest()
                    {
                        SecurityKey = _securityKey,
                        ClientSessionId = _clientSessionId,
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

                        _messageHubConnection.SendMessage(request, _remoteEndpointInfo);

                        // Wait for response                        
                        responsesSession.CancellationTokenSource.CancelAfter(_responseTimeout);
                        var responseMessages = await WaitForResponsesAsync(request, responsesSession);

                        // Check response
                        ThrowResponseExceptionIfRequired(responseMessages.FirstOrDefault());

                        var response = (GetMessageHubsResponse)responseMessages.First();
                        return response.MessageHubs;
                    }
                    catch (MessageConnectionException messageConnectionException)
                    {
                        throw new MessageQueueException("Error getting message hubs", messageConnectionException);
                    }
                }
            });
        }    

        public async Task<List<MessageQueue>> GetMessageQueuesAsync()
        {
            return await Task.Run(async () =>
            {
                using (var disposableSession = new DisposableSession())
                {
                    var request = new GetMessageQueuesRequest()
                    {
                        SecurityKey = _securityKey,
                        ClientSessionId = _clientSessionId,
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

                        _messageHubConnection.SendMessage(request, _remoteEndpointInfo);

                        // Wait for response                        
                        responsesSession.CancellationTokenSource.CancelAfter(_responseTimeout);
                        var responseMessages = await WaitForResponsesAsync(request, responsesSession);

                        // Check response
                        ThrowResponseExceptionIfRequired(responseMessages.FirstOrDefault());

                        var response = (GetMessageQueuesResponse)responseMessages.First();
                        return response.MessageQueues;
                    }
                    catch (MessageConnectionException messageConnectionException)
                    {
                        throw new MessageQueueException("Error getting message queues", messageConnectionException);
                    }
                }
            });
        }      
    }
}
