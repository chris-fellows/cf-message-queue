using CFConnectionMessaging.Models;
using CFMessageQueue.Constants;
using CFMessageQueue.Enums;
using CFMessageQueue.Hub.Enums;
using CFMessageQueue.Hub.Models;
using CFMessageQueue.Interfaces;
using CFMessageQueue.Logging;
using CFMessageQueue.Logs;
using CFMessageQueue.Models;
using CFMessageQueue.Utilities;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace CFMessageQueue.Hub
{
    /// <summary>
    /// Worker that handles single message queue.
    /// </summary>
    public class MessageQueueWorker
    {
        private readonly MessageHubClientsConnection _clientsConnection;

        private readonly ConcurrentQueue<QueueItem> _queueItems = new();

        private readonly System.Timers.Timer _timer;   

        private readonly List<QueueItemTask> _queueItemTasks = new List<QueueItemTask>();

        private readonly MessageQueue _messageQueue;

        private readonly IServiceProvider _serviceProvider;

        private DateTimeOffset _messageHubClientsLastRefresh = DateTimeOffset.MinValue;
        private readonly Dictionary<string, MessageHubClient> _messageHubClientsBySecurityKey = new();

        private TimeSpan _expireOldMessagesFrequency = TimeSpan.FromSeconds(60);
        private DateTimeOffset _lastExpireOldMessages = DateTimeOffset.MinValue;

        private TimeSpan _expiredProcessingMessagesFrequency = TimeSpan.FromSeconds(60);
        private DateTimeOffset _lastExpireProcessingMessages = DateTimeOffset.MinValue;

        private TimeSpan _logStatisticsFrequency = TimeSpan.FromSeconds(60);
        private DateTimeOffset _lastLogStatistics = DateTimeOffset.MinValue;

        private readonly ISimpleLog _log;
        private readonly IAuditLog _auditLog;

        private readonly Mutex _queueMutex = new Mutex();

        /// <summary>
        /// Client subscriptions for queue. Each subscription refers to a specific instance of MessageQueueClientConnector.
        /// There may be multiple instances of MessageQueueClientConnector for a single message hub client.
        /// 
        /// TODO: Consider cleaning this up if client disconnects
        /// </summary>
        private readonly List<ClientQueueSubscription> _clientQueueSubscriptions = new();

        /// <summary>
        /// Queue messages currently being processed
        /// </summary>
        private readonly List<QueueMessageInternal> _queueMessageInternalProcessing = new();

        public MessageQueueWorker(MessageQueue messageQueue, IServiceProvider serviceProvider)
        {
            _messageQueue = messageQueue;
            _serviceProvider = serviceProvider;
            _log = serviceProvider.GetRequiredService<ISimpleLog>();
            _auditLog = serviceProvider.GetRequiredService<IAuditLog>();

            _clientsConnection = new MessageHubClientsConnection(serviceProvider);            

            // Handle connection message received
            _clientsConnection.OnMessageReceived += delegate (MessageBase message, MessageReceivedInfo messageReceivedInfo)
            {
                _log.Log(DateTimeOffset.UtcNow, "Information", $"Received message {message.TypeId} from {messageReceivedInfo.RemoteEndpointInfo.Ip}:{messageReceivedInfo.RemoteEndpointInfo.Port}");

                var queueItem = new QueueItem()
                {
                    ItemType = QueueItemTypes.ExternalMessage,
                    Message = message,
                    MessageReceivedInfo = messageReceivedInfo
                };
                _queueItems.Enqueue(queueItem);

                _timer.Interval = 100;
            };

            // Handle client connection
            _clientsConnection.OnClientDisconnected += delegate (EndpointInfo endpointInfo)
            {
                _log.Log(DateTimeOffset.UtcNow, "Information", $"Queue client connected {endpointInfo.Ip}:{endpointInfo.Port}");
            };

            // Handle client disconnection
            _clientsConnection.OnClientDisconnected += delegate (EndpointInfo endpointInfo)
            {
                _log.Log(DateTimeOffset.UtcNow, "Information", $"Queue client disconnected {endpointInfo.Ip}:{endpointInfo.Port}");

                // Remove subscriptions
                _clientQueueSubscriptions.RemoveAll(s => s.RemoteEndpointInfo.Ip == endpointInfo.Ip && s.RemoteEndpointInfo.Port == endpointInfo.Port);
            };

            _timer = new System.Timers.Timer();
            _timer.Elapsed += _timer_Elapsed;
            _timer.Interval = 5000;
            _timer.Enabled = false;
        }

        public void NotifyQueueCleared()
        {
            _clientQueueSubscriptions.ForEach(c =>
            {
                c.DoNotifyQueueCleared = true;
                c.DoNotifyMessageAdded = false; // Sanity check
                c.NotifyIfMessageAdded = false; // Sanity check
            });
        }

        public string MessageQueueId => _messageQueue.Id;

        public Mutex QueueMutex => _queueMutex;
        
        public void Start()
        {
            _log.Log(DateTimeOffset.UtcNow, "Information", "Worker starting");

            _timer.Enabled = true;

            _clientsConnection.StartListening(_messageQueue.Port);
        }

        public void Stop()
        {
            _log.Log(DateTimeOffset.UtcNow, "Information", "Worker stopping");

            _timer.Enabled = false;

            _clientsConnection.StopListening();
        }

        private void _timer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                _timer.Enabled = false;

                CheckCompleteQueueItemTasks(_queueItemTasks);

                // Check if need to expire messages
                if (_lastExpireOldMessages.Add(_expireOldMessagesFrequency) <= DateTimeOffset.UtcNow &&
                    !_queueItems.Any(q => q.ItemType == QueueItemTypes.ExpireOldQueueMessages)) 
                {
                    _queueItems.Enqueue(new QueueItem() { ItemType = QueueItemTypes.ExpireOldQueueMessages });
                }                

                // Check if need to expire processing queue messages
                if (_lastExpireProcessingMessages.Add(_expiredProcessingMessagesFrequency) <= DateTimeOffset.UtcNow &&
                      !_queueItems.Any(q => q.ItemType == QueueItemTypes.ExpireProcessingQueueMessages))
                {
                    _queueItems.Enqueue(new QueueItem() { ItemType = QueueItemTypes.ExpireProcessingQueueMessages });
                }
                
                // Check if need to log statistics
                if (_lastLogStatistics.Add(_logStatisticsFrequency) <= DateTimeOffset.UtcNow &&
                      !_queueItems.Any(q => q.ItemType == QueueItemTypes.LogQueueStatistics))
                {
                    _queueItems.Enqueue(new QueueItem() { ItemType = QueueItemTypes.LogQueueStatistics });
                }

                _clientQueueSubscriptions.Where(c => c.QueueSizeFrequencySecs > 0 &&
                                    c.LastNotifyQueueSize.AddSeconds(c.QueueSizeFrequencySecs) <= DateTimeOffset.UtcNow).ToList()
                                    .ForEach(s =>
                                    {
                                        s.DoNotifyQueueSize = true;
                                    });

                // Check if need to notify subscriptions
                if (_clientQueueSubscriptions.Any(s => s.IsNotificationRequired) &&
                    !_queueItems.Any(q => q.ItemType == QueueItemTypes.QueueNotifications))
                {
                    _queueItems.Enqueue(new QueueItem() { ItemType = QueueItemTypes.QueueNotifications });
                }

                ProcessQueueItems(() =>
                {
                    // Periodic action to do while processing queue items
                });

                CheckCompleteQueueItemTasks(_queueItemTasks);
            }
            catch(Exception exception)
            {
                _log.Log(DateTimeOffset.UtcNow, "Error", $"Error executing regular functions: {exception.Message}");
            }
            finally
            {
                _timer.Interval = _queueItems.Any() ||
                            _queueItemTasks.Any()  ||
                            _clientQueueSubscriptions.Any(s => s.IsNotificationRequired) ? 100 : 5000;
                _timer.Enabled = true;
            }
        }

        private void ProcessQueueItems(Action periodicAction)
        {
            while (_queueItems.Any())
            {                
                    if (_queueItems.TryDequeue(out QueueItem queueItem))
                    {
                        ProcessQueueItem(queueItem);
                    }             

                    periodicAction();
            }
        }

        /// <summary>
        /// Processes queue item
        /// </summary>
        /// <param name="queueItem"></param>
        private void ProcessQueueItem(QueueItem queueItem)
        {
            if (queueItem.ItemType == QueueItemTypes.ExternalMessage && queueItem.Message != null)
            {
                switch(queueItem.Message.TypeId)
                {
                    case MessageTypeIds.AddQueueMessageRequest:                        
                        _queueItemTasks.Add(new QueueItemTask(HandleAddQueueMessageRequestAsync((AddQueueMessageRequest)queueItem.Message, queueItem.MessageReceivedInfo), queueItem));
                        break;

                    case MessageTypeIds.GetNextQueueMessageRequest:                        
                        _queueItemTasks.Add(new QueueItemTask(HandleGetNextQueueMessageRequestAsync((GetNextQueueMessageRequest)queueItem.Message, queueItem.MessageReceivedInfo), queueItem));
                        break;

                    case MessageTypeIds.GetQueueMessagesRequest:                        
                        _queueItemTasks.Add(new QueueItemTask(HandleGetQueueMessagesRequestAsync((GetQueueMessagesRequest)queueItem.Message, queueItem.MessageReceivedInfo), queueItem));
                        break;

                    case MessageTypeIds.MessageQueueSubscribeRequest:                        
                        _queueItemTasks.Add(new QueueItemTask(HandleMessageQueueSubscribeRequestAsync((MessageQueueSubscribeRequest)queueItem.Message, queueItem.MessageReceivedInfo), queueItem));
                        break;

                    case MessageTypeIds.QueueMessageProcessedRequest:
                        _queueItemTasks.Add(new QueueItemTask(HandleQueueMessageProcessedRequestAsync((QueueMessageProcessedRequest)queueItem.Message, queueItem.MessageReceivedInfo), queueItem));
                        break;
                }
            }
            else if (queueItem.ItemType == QueueItemTypes.ExpireOldQueueMessages)
            {
                _queueItemTasks.Add(new QueueItemTask(ExpireOldQueueMessagesAsync(_messageQueue.Id), queueItem));
            }
            else if (queueItem.ItemType == QueueItemTypes.ExpireProcessingQueueMessages)
            {
                _queueItemTasks.Add(new QueueItemTask(ExpiredProcessingMessagesAsync(_messageQueue.Id), queueItem));
            }
            else if (queueItem.ItemType == QueueItemTypes.LogQueueStatistics)
            {
                _queueItemTasks.Add(new QueueItemTask(LogStatisticsTaskAsync(_messageQueue.Id), queueItem));
            }
            else if (queueItem.ItemType == QueueItemTypes.QueueNotifications)
            {
                _queueItemTasks.Add(new QueueItemTask(NotifySubscriptionsAsync(), queueItem));
            }            
        }

        private Task HandleQueueMessageProcessedRequestAsync(QueueMessageProcessedRequest queueMessageProcessedRequest, MessageReceivedInfo messageReceivedInfo)
        {
            return Task.Run(async () =>
            {
                var isHasMutex = false;

                _log.Log(DateTimeOffset.UtcNow, "Information", $"Processing {queueMessageProcessedRequest.TypeId}");

                var response = new QueueMessageProcessedResponse()
                {
                    Response = new MessageResponse()
                    {
                        IsMore = false,
                        MessageId = queueMessageProcessedRequest.Id,
                        Sequence = 1
                    },
                };

                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var messageHubClientService = scope.ServiceProvider.GetRequiredService<IMessageHubClientService>();
                        var messageQueueService = scope.ServiceProvider.GetRequiredService<IMessageQueueService>();
                        var queueMessageService = scope.ServiceProvider.GetRequiredService<IQueueMessageInternalService>();                     

                        var messageHubClient = GetMessageHubClientBySecurityKey(queueMessageProcessedRequest.SecurityKey, messageHubClientService);

                        var messageQueue = await messageQueueService.GetByIdAsync(queueMessageProcessedRequest.MessageQueueId);

                        if (messageHubClient == null)
                        {
                            response.Response.ErrorCode = ResponseErrorCodes.PermissionDenied;
                            response.Response.ErrorMessage = EnumUtilities.GetEnumDescription(response.Response.ErrorCode);
                        }
                        else if (messageQueue == null)
                        {
                            response.Response.ErrorCode = ResponseErrorCodes.InvalidParameters;
                            response.Response.ErrorMessage = $"{EnumUtilities.GetEnumDescription(response.Response.ErrorCode)}: Message Queue is invalid";
                        }
                        else
                        {
                            isHasMutex = _queueMutex.WaitOne();

                            // Check that they're processing this message
                            var queueMessageMemory = _queueMessageInternalProcessing.FirstOrDefault(m => m.Id == queueMessageProcessedRequest.QueueMessageId);
                            if (queueMessageMemory == null)     // Not processing message
                            {
                                response.Response.ErrorCode = ResponseErrorCodes.InvalidParameters;
                                response.Response.ErrorMessage = $"{EnumUtilities.GetEnumDescription(response.Response.ErrorCode)}: Queue Message is not being processed";
                            }
                            else if (queueMessageMemory.ProcessingMessageHubClientId != messageHubClient.Id)   // Another client is processing
                            {
                                response.Response.ErrorCode = ResponseErrorCodes.InvalidParameters;
                                response.Response.ErrorMessage = $"{EnumUtilities.GetEnumDescription(response.Response.ErrorCode)}: Queue Message is being processed by another client";
                            }
                            else
                            {
                                if (queueMessageProcessedRequest.Processed)
                                {
                                    // Delete queue message
                                    await queueMessageService.DeleteByIdAsync(queueMessageProcessedRequest.QueueMessageId);

                                    // Log processed
                                    _auditLog.LogQueueMessage(DateTimeOffset.UtcNow, "PROCESSED", _messageQueue.Name, queueMessageMemory);
                                }
                                else
                                {
                                    // Reset queue message status
                                    var queueMessage = await queueMessageService.GetByIdAsync(queueMessageProcessedRequest.QueueMessageId);

                                    queueMessage.Status = QueueMessageStatuses.Default;
                                    queueMessage.ProcessingMessageHubClientId = "";
                                    queueMessage.ProcessingStartDateTime = DateTimeOffset.MinValue;
                                    queueMessage.MaxProcessingSeconds = 0;

                                    await queueMessageService.UpdateAsync(queueMessage);
                                }

                               _queueMessageInternalProcessing.Remove(queueMessageMemory);
                            }
                        }                        
                    }
                }
                catch (Exception exception)
                {
                    response.Response.ErrorCode = ResponseErrorCodes.Unknown;
                    response.Response.ErrorMessage = $"{EnumUtilities.GetEnumDescription(response.Response.ErrorCode)}: {exception.Message}";

                    _log.Log(DateTimeOffset.UtcNow, "Error", $"Error processing {queueMessageProcessedRequest.TypeId}: {exception.Message}");
                }
                finally
                {
                    if (isHasMutex) _queueMutex.ReleaseMutex();

                    // Send response
                    _clientsConnection.SendMessage(response, messageReceivedInfo.RemoteEndpointInfo);

                    _log.Log(DateTimeOffset.UtcNow, "Information", $"Processed {queueMessageProcessedRequest.TypeId} ({(response.Response.ErrorCode == null ? "Success" : response.Response.ErrorMessage)})");                    
                }                
            });
        }

        /// <summary>
        /// Handles request to add queue message
        /// </summary>
        /// <param name="addQueueMessageRequest"></param>
        /// <param name="messageReceivedInfo"></param>
        /// <returns></returns>
        private Task HandleAddQueueMessageRequestAsync(AddQueueMessageRequest addQueueMessageRequest, MessageReceivedInfo messageReceivedInfo)
        {
            return Task.Run(async () =>
            {
                _log.Log(DateTimeOffset.UtcNow, "Information", $"Processing {addQueueMessageRequest.TypeId}");

                var isHasMutex = false;

                var response = new AddQueueMessageResponse()
                {
                    Response = new MessageResponse()
                    {
                        IsMore = false,
                        MessageId = addQueueMessageRequest.Id,
                        Sequence = 1
                    },
                };                

                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var messageHubClientService = scope.ServiceProvider.GetRequiredService<IMessageHubClientService>();
                        var messageQueueService = scope.ServiceProvider.GetRequiredService<IMessageQueueService>();
                        var queueMessageService = scope.ServiceProvider.GetRequiredService<IQueueMessageInternalService>();
                        
                        var messageHubClient = GetMessageHubClientBySecurityKey(addQueueMessageRequest.SecurityKey, messageHubClientService);
                        
                        var messageQueue = await messageQueueService.GetByIdAsync(addQueueMessageRequest.MessageQueueId);

                        var securityItem = (messageQueue == null || messageHubClient == null) ? 
                                        null : messageQueue.SecurityItems.FirstOrDefault(si => si.MessageHubClientId == messageHubClient.Id);

                        if (messageHubClient == null)
                        {
                            response.Response.ErrorCode = ResponseErrorCodes.PermissionDenied;
                            response.Response.ErrorMessage = EnumUtilities.GetEnumDescription(response.Response.ErrorCode);
                        }
                        else if (messageQueue == null)
                        {
                            response.Response.ErrorCode = ResponseErrorCodes.InvalidParameters;
                            response.Response.ErrorMessage = $"{EnumUtilities.GetEnumDescription(response.Response.ErrorCode)}: Message Queue is invalid";
                        }
                        else if (securityItem == null || !securityItem.RoleTypes.Contains(RoleTypes.QueueWriteQueue))
                        {
                            response.Response.ErrorCode = ResponseErrorCodes.PermissionDenied;
                            response.Response.ErrorMessage = EnumUtilities.GetEnumDescription(response.Response.ErrorCode);
                        }
                        else if (String.IsNullOrWhiteSpace(addQueueMessageRequest.QueueMessage.TypeId))
                        {
                            response.Response.ErrorCode = ResponseErrorCodes.InvalidParameters;
                            response.Response.ErrorMessage = $"{EnumUtilities.GetEnumDescription(response.Response.ErrorCode)}: Type Id must be set";
                        }
                        else if (addQueueMessageRequest.QueueMessage.ExpirySeconds < 0)
                        {
                            response.Response.ErrorCode = ResponseErrorCodes.InvalidParameters;
                            response.Response.ErrorMessage = $"{EnumUtilities.GetEnumDescription(response.Response.ErrorCode)}: Expiry Seconds is invalid";
                        }                        
                        else
                        {
                            isHasMutex = _queueMutex.WaitOne();

                            // Check if queue is full                            
                            var isQueueFull = false;
                            if (messageQueue.MaxSize > 0)
                            {
                                var messageCount = (await queueMessageService.GetByMessageQueueAsync(messageQueue.Id)).Count;

                                isQueueFull = messageCount >= messageQueue.MaxSize;
                            }

                            if (isQueueFull)
                            {
                                response.Response.ErrorCode = ResponseErrorCodes.MessageQueueFull;
                                response.Response.ErrorMessage = EnumUtilities.GetEnumDescription(response.Response.ErrorCode);
                            }
                            else
                            {
                                // Create queue message
                                var queueMessage = new QueueMessageInternal()
                                {
                                    Content = addQueueMessageRequest.QueueMessage.Content,
                                    ContentType = addQueueMessageRequest.QueueMessage.ContentType,
                                    CreatedDateTime = DateTime.UtcNow,                                    
                                    ExpirySeconds = addQueueMessageRequest.QueueMessage.ExpirySeconds,
                                    ExpiryDateTime = addQueueMessageRequest.QueueMessage.ExpirySeconds == 0 ? 
                                                DateTime.MaxValue : DateTime.UtcNow.AddSeconds(addQueueMessageRequest.QueueMessage.ExpirySeconds),
                                    Id = Guid.NewGuid().ToString(),
                                    MessageQueueId = addQueueMessageRequest.MessageQueueId,
                                    Name = addQueueMessageRequest.QueueMessage.Name,
                                    Priority = addQueueMessageRequest.QueueMessage.Priority,
                                    SenderMessageHubClientId = messageHubClient.Id,
                                    Status = QueueMessageStatuses.Default,
                                    TypeId = addQueueMessageRequest.QueueMessage.TypeId
                                };

                                await queueMessageService.AddAsync(queueMessage);

                                // Log added
                                _auditLog.LogQueueMessage(DateTimeOffset.UtcNow, "ADDED", _messageQueue.Name, queueMessage);

                                // Set QueueMessageId for response
                                response.QueueMessageId = queueMessage.Id;

                                // If client subscription indicates waiting for new message then flag notification
                                _clientQueueSubscriptions.Where(s => s.NotifyIfMessageAdded).ToList()
                                        .ForEach(s =>
                                        {
                                            s.DoNotifyMessageAdded = true;
                                            s.NotifyIfMessageAdded = false;
                                        });
                            }
                        }                                           
                    }
                }
                catch (Exception exception)
                {
                    response.Response.ErrorCode = ResponseErrorCodes.Unknown;
                    response.Response.ErrorMessage = $"{EnumUtilities.GetEnumDescription(response.Response.ErrorCode)}: {exception.Message}";

                    _log.Log(DateTimeOffset.UtcNow, "Error", $"Error processing {addQueueMessageRequest.TypeId}: {exception.Message}");
                }
                finally
                {
                    if (isHasMutex) _queueMutex.ReleaseMutex();

                    // Send response
                    _clientsConnection.SendMessage(response, messageReceivedInfo.RemoteEndpointInfo);

                    _log.Log(DateTimeOffset.UtcNow, "Information", $"Processed {addQueueMessageRequest.TypeId} ({(response.Response.ErrorCode == null ? "Success" : response.Response.ErrorMessage)})");                    
                }                
            });
        }

        /// <summary>
        /// Handles request to get next queue message
        /// </summary>
        /// <param name="getNextQueueMessageRequest"></param>
        /// <param name="messageReceivedInfo"></param>
        /// <returns></returns>
        private Task HandleGetNextQueueMessageRequestAsync(GetNextQueueMessageRequest getNextQueueMessageRequest, MessageReceivedInfo messageReceivedInfo)
        {
            return Task.Run(async () =>
            {
                _log.Log(DateTimeOffset.UtcNow, "Information", $"Processing {getNextQueueMessageRequest.TypeId}");

                var isHasMutex = false;

                var response = new GetNextQueueMessageResponse()
                {
                    Response = new MessageResponse()
                    {
                        IsMore = false,
                        MessageId = getNextQueueMessageRequest.Id,
                        Sequence = 1
                    },
                };

                try
                {                    
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var messageHubClientService = scope.ServiceProvider.GetRequiredService<IMessageHubClientService>();
                        var messageQueueService = scope.ServiceProvider.GetRequiredService<IMessageQueueService>();
                        var queueMessageService = scope.ServiceProvider.GetRequiredService<IQueueMessageInternalService>();
                     
                        var messageHubClient = GetMessageHubClientBySecurityKey(getNextQueueMessageRequest.SecurityKey, messageHubClientService);

                        var messageQueue = await messageQueueService.GetByIdAsync(getNextQueueMessageRequest.MessageQueueId);

                        var securityItem = (messageQueue == null || messageHubClient == null) ?
                                        null : messageQueue.SecurityItems.FirstOrDefault(si => si.MessageHubClientId == messageHubClient.Id);

                        if (messageHubClient == null)
                        {
                            response.Response.ErrorCode = ResponseErrorCodes.PermissionDenied;
                            response.Response.ErrorMessage = EnumUtilities.GetEnumDescription(response.Response.ErrorCode);
                        }
                        else if (messageQueue == null)
                        {
                            response.Response.ErrorCode = ResponseErrorCodes.InvalidParameters;
                            response.Response.ErrorMessage = $"{EnumUtilities.GetEnumDescription(response.Response.ErrorCode)}: Message Queue is invalid";
                        }
                        else if (securityItem == null || !securityItem.RoleTypes.Contains(RoleTypes.QueueReadQueue))
                        {
                            response.Response.ErrorCode = ResponseErrorCodes.PermissionDenied;
                            response.Response.ErrorMessage = EnumUtilities.GetEnumDescription(response.Response.ErrorCode);
                        }
                        else if (getNextQueueMessageRequest.MaxProcessingSeconds < 1)
                        {
                            response.Response.ErrorCode = ResponseErrorCodes.InvalidParameters;
                            response.Response.ErrorMessage = $"{EnumUtilities.GetEnumDescription(response.Response.ErrorCode)}: Message Processing Seconds must be 1 second or more";
                        }
                        else
                        {                            
                            // Try and get message, if no message then may need to wait for message
                            bool waitedForMessage = false;
                            QueueMessageInternal? queueMessage = null;
                            var stopwatch = new Stopwatch();    // Started on first failure to get a message from DB
                            while (!waitedForMessage)
                            {
                                // Get mutex. Need to hold it for shortest time possible
                                isHasMutex = _queueMutex.WaitOne();

                                // Get next message. Only allowed if not exceeded max number of concurrent messages being processed                             
                                if (messageQueue.MaxConcurrentProcessing == 0 ||
                                    _queueMessageInternalProcessing.Count < messageQueue.MaxConcurrentProcessing)
                                {                                    
                                    queueMessage = await queueMessageService.GetNextAsync(messageQueue.Id);                                    
                                }

                                if (queueMessage != null)
                                {                                    
                                    // Update message as Processing
                                    queueMessage.Status = QueueMessageStatuses.Processing;
                                    queueMessage.ProcessingMessageHubClientId = messageHubClient.Id;
                                    queueMessage.MaxProcessingSeconds = getNextQueueMessageRequest.MaxProcessingSeconds;
                                    queueMessage.ProcessingStartDateTime = DateTimeOffset.UtcNow;

                                    await queueMessageService.UpdateAsync(queueMessage);

                                    // Set message as processing
                                    _queueMessageInternalProcessing.Add(queueMessage);

                                    waitedForMessage = true;                                    
                                }

                                // Release mutex
                                _queueMutex.ReleaseMutex();
                                isHasMutex = false;

                                // Handle no message
                                if (queueMessage == null)    // No message
                                {
                                    if (getNextQueueMessageRequest.MaxWaitMilliseconds == 0)   // No wait
                                    {
                                        waitedForMessage = true;
                                    }
                                    else      // Wait until message or timeout
                                    {
                                        if (stopwatch.IsRunning)
                                        {
                                            if (stopwatch.ElapsedMilliseconds < getNextQueueMessageRequest.MaxWaitMilliseconds)
                                            {
                                                Thread.Sleep(250);
                                            }
                                            else     // Timeout
                                            {
                                                waitedForMessage = true;                                                
                                            }
                                        }
                                        else   // Start stopwatch for first time
                                        {                                            
                                            stopwatch.Start();
                                        }
                                    }
                                }
                            }

                            response.QueueMessage = queueMessage;

                            // If there's no message then we should notify subscriptions when new message is added
                            if (queueMessage == null)
                            {
                                _clientQueueSubscriptions.ForEach(s =>
                                {
                                    s.NotifyIfMessageAdded = true;
                                    s.DoNotifyMessageAdded = false;
                                });
                            }
                        }                        
                    }
                }
                catch (Exception exception)
                {
                    response.Response.ErrorCode = ResponseErrorCodes.Unknown;
                    response.Response.ErrorMessage = $"{EnumUtilities.GetEnumDescription(response.Response.ErrorCode)}: {exception.Message}";

                    _log.Log(DateTimeOffset.UtcNow, "Error", $"Error processing {getNextQueueMessageRequest.TypeId}: {exception.Message}");
                }
                finally
                {
                    if (isHasMutex) _queueMutex.ReleaseMutex();

                    // Send response
                    _clientsConnection.SendMessage(response, messageReceivedInfo.RemoteEndpointInfo);

                    _log.Log(DateTimeOffset.UtcNow, "Information", $"Processed {getNextQueueMessageRequest.TypeId} ({(response.Response.ErrorCode == null ? "Success" : response.Response.ErrorMessage)})");                    
                }
            });
        }


        /// <summary>
        /// Handles request to get queue messages page
        /// </summary>
        /// <param name="getNextQueueMessageRequest"></param>
        /// <param name="messageReceivedInfo"></param>
        /// <returns></returns>
        private Task HandleGetQueueMessagesRequestAsync(GetQueueMessagesRequest getQueueMessagesRequest, MessageReceivedInfo messageReceivedInfo)
        {
            return Task.Run(async () =>
            {
                _log.Log(DateTimeOffset.UtcNow, "Information", $"Processing {getQueueMessagesRequest.TypeId}");

                var response = new GetQueueMessagesResponse()
                {
                    Response = new MessageResponse()
                    {
                        IsMore = false,
                        MessageId = getQueueMessagesRequest.Id,
                        Sequence = 1
                    },
                };

                var isHasMutex = false;
                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var messageHubClientService = scope.ServiceProvider.GetRequiredService<IMessageHubClientService>();
                        var messageQueueService = scope.ServiceProvider.GetRequiredService<IMessageQueueService>();
                        var queueMessageService = scope.ServiceProvider.GetRequiredService<IQueueMessageInternalService>();

                        var messageHubClient = GetMessageHubClientBySecurityKey(getQueueMessagesRequest.SecurityKey, messageHubClientService);

                        var messageQueue = await messageQueueService.GetByIdAsync(getQueueMessagesRequest.MessageQueueId);

                        var securityItem = (messageQueue == null || messageHubClient == null) ?
                                        null : messageQueue.SecurityItems.FirstOrDefault(si => si.MessageHubClientId == messageHubClient.Id);

                        if (messageHubClient == null)
                        {
                            response.Response.ErrorCode = ResponseErrorCodes.PermissionDenied;
                            response.Response.ErrorMessage = EnumUtilities.GetEnumDescription(response.Response.ErrorCode);
                        }
                        else if (messageQueue == null)
                        {
                            response.Response.ErrorCode = ResponseErrorCodes.InvalidParameters;
                            response.Response.ErrorMessage = $"{EnumUtilities.GetEnumDescription(response.Response.ErrorCode)}: Message Queue is invalid";
                        }
                        else if (getQueueMessagesRequest.Page < 1 || getQueueMessagesRequest.PageItems < 1)
                        {
                            response.Response.ErrorCode = ResponseErrorCodes.InvalidParameters;
                            response.Response.ErrorMessage = EnumUtilities.GetEnumDescription(response.Response.ErrorCode);
                        }
                        else if (getQueueMessagesRequest.PageItems > 500)   // Limit page size requests
                        {
                            response.Response.ErrorCode = ResponseErrorCodes.InvalidParameters;
                            response.Response.ErrorMessage = $"{EnumUtilities.GetEnumDescription(response.Response.ErrorCode)}: Page Items must be 500 or less";
                        }
                        else if (securityItem == null || !securityItem.RoleTypes.Contains(RoleTypes.QueueReadQueue))
                        {
                            response.Response.ErrorCode = ResponseErrorCodes.PermissionDenied;
                            response.Response.ErrorMessage = EnumUtilities.GetEnumDescription(response.Response.ErrorCode);
                        }
                        else
                        {
                            // Get page of messages
                            // TODO: Return page count
                            response.QueueMessages = (await queueMessageService.GetByMessageQueueAsync(getQueueMessagesRequest.MessageQueueId))
                                                .OrderBy(m => m.CreatedDateTime)
                                                .Skip((getQueueMessagesRequest.Page - 1) * getQueueMessagesRequest.PageItems)
                                                .Take(getQueueMessagesRequest.PageItems)
                                                .ToList();
                        }
                    }
                }
                catch (Exception exception)
                {
                    response.Response.ErrorCode = ResponseErrorCodes.Unknown;
                    response.Response.ErrorMessage = $"{EnumUtilities.GetEnumDescription(response.Response.ErrorCode)}: {exception.Message}";

                    _log.Log(DateTimeOffset.UtcNow, "Error", $"Error processing {getQueueMessagesRequest.TypeId}: {exception.Message}");
                }
                finally
                {
                    if (isHasMutex) _queueMutex.ReleaseMutex();

                    // Send response
                    _clientsConnection.SendMessage(response, messageReceivedInfo.RemoteEndpointInfo);

                    _log.Log(DateTimeOffset.UtcNow, "Information", $"Processed {getQueueMessagesRequest.TypeId} ({(response.Response.ErrorCode == null ? "Success" : response.Response.ErrorMessage)})");                    
                }                
            });
        }

        /// <summary>
        /// Handles message queue subscribe request
        /// </summary>
        /// <param name="getNextQueueMessageRequest"></param>
        /// <param name="messageReceivedInfo"></param>
        /// <returns></returns>
        private Task HandleMessageQueueSubscribeRequestAsync(MessageQueueSubscribeRequest messageQueueSubscribeRequest, MessageReceivedInfo messageReceivedInfo)
        {
            return Task.Run(async () =>
            {
                _log.Log(DateTimeOffset.UtcNow, "Information", $"Processing {messageQueueSubscribeRequest.TypeId}");

                // Create response
                var response = new MessageQueueSubscribeResponse()
                {
                    Response = new MessageResponse()
                    {
                        IsMore = false,
                        MessageId = messageQueueSubscribeRequest.Id,
                        Sequence = 1
                    },
                    SubscribeId = ""
                };

                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var messageHubClientService = scope.ServiceProvider.GetRequiredService<IMessageHubClientService>();
                        var messageQueueService = scope.ServiceProvider.GetRequiredService<IMessageQueueService>();
                        var queueMessageService = scope.ServiceProvider.GetRequiredService<IQueueMessageInternalService>();
                        //var messageQueueSubscriptionService = scope.ServiceProvider.GetRequiredService<IMessageQueueSubscriptionService>();

                        var messageHubClient = GetMessageHubClientBySecurityKey(messageQueueSubscribeRequest.SecurityKey, messageHubClientService);

                        var messageQueue = await messageQueueService.GetByIdAsync(messageQueueSubscribeRequest.MessageQueueId);

                        var securityItem = (messageQueue == null || messageHubClient == null) ?
                                         null : messageQueue.SecurityItems.FirstOrDefault(si => si.MessageHubClientId == messageHubClient.Id);

                        if (messageHubClient == null)
                        {
                            response.Response.ErrorCode = ResponseErrorCodes.PermissionDenied;
                            response.Response.ErrorMessage = EnumUtilities.GetEnumDescription(response.Response.ErrorCode);
                        }
                        else if (messageQueue == null)
                        {
                            response.Response.ErrorCode = ResponseErrorCodes.InvalidParameters;
                            response.Response.ErrorMessage = $"{EnumUtilities.GetEnumDescription(response.Response.ErrorCode)}: Message Queue is invalid";
                        }
                        else if (securityItem == null || !securityItem.RoleTypes.Contains(RoleTypes.QueueSubscribeQueue))
                        {
                            response.Response.ErrorCode = ResponseErrorCodes.PermissionDenied;
                            response.Response.ErrorMessage = EnumUtilities.GetEnumDescription(response.Response.ErrorCode);
                        }
                        else if (messageQueueSubscribeRequest.QueueSizeFrequencySecs > 0 &&
                                messageQueueSubscribeRequest.QueueSizeFrequencySecs < 10)       // Limit smallest duration
                        {
                            response.Response.ErrorCode = ResponseErrorCodes.InvalidParameters;
                            response.Response.ErrorMessage = $"{EnumUtilities.GetEnumDescription(response.Response.ErrorCode)}: Queue Frequency Size Seconds must be 10 seconds or more ";
                        }
                        else
                        {
                            // Check for existing subscription                        
                            var clientQueueSubscription = _clientQueueSubscriptions.FirstOrDefault(s => s.Id == messageQueueSubscribeRequest.ClientSessionId);

                            // Handle subscribe or unsubscribe
                            switch (messageQueueSubscribeRequest.ActionName)
                            {
                                case "SUBSCRIBE":
                                    if (clientQueueSubscription == null)   // New subscription
                                    {
                                        clientQueueSubscription = new ClientQueueSubscription()
                                        {
                                            Id = Guid.NewGuid().ToString(),
                                            ClientSessionId = messageQueueSubscribeRequest.ClientSessionId,
                                            MessageHubClientId = messageHubClient.Id,
                                            RemoteEndpointInfo = messageReceivedInfo.RemoteEndpointInfo,
                                            QueueSizeFrequencySecs = messageQueueSubscribeRequest.QueueSizeFrequencySecs
                                        };
                                        _clientQueueSubscriptions.Add(clientQueueSubscription);
                                    }
                                    else    // Update subscription
                                    {
                                        clientQueueSubscription.ClientSessionId = messageQueueSubscribeRequest.ClientSessionId;
                                        clientQueueSubscription.RemoteEndpointInfo = messageReceivedInfo.RemoteEndpointInfo;
                                        clientQueueSubscription.QueueSizeFrequencySecs = messageQueueSubscribeRequest.QueueSizeFrequencySecs;
                                    }

                                    response.SubscribeId = clientQueueSubscription.Id;
                                    break;

                                case "UNSUBSCRIBE":
                                    if (clientQueueSubscription != null)
                                    {
                                        _clientQueueSubscriptions.Remove(clientQueueSubscription);
                                    }
                                    break;

                                default:
                                    response.Response.ErrorCode = ResponseErrorCodes.InvalidParameters;
                                    response.Response.ErrorMessage = $"{EnumUtilities.GetEnumDescription(response.Response.ErrorCode)}: Action Name {messageQueueSubscribeRequest.ActionName} is invalid";
                                    break;
                            }
                        }
                    }
                }
                catch (Exception exception)
                {
                    response.Response.ErrorCode = ResponseErrorCodes.Unknown;
                    response.Response.ErrorMessage = $"{EnumUtilities.GetEnumDescription(response.Response.ErrorCode)}: {exception.Message}";

                    _log.Log(DateTimeOffset.UtcNow, "Error", $"Error processing {messageQueueSubscribeRequest.TypeId}: {exception.Message}");
                }
                finally
                {
                    // Send response
                    _clientsConnection.SendMessage(response, messageReceivedInfo.RemoteEndpointInfo);

                    _log.Log(DateTimeOffset.UtcNow, "Information", $"Processed {messageQueueSubscribeRequest.TypeId} ({(response.Response.ErrorCode == null ? "Success" : response.Response.ErrorMessage)})");                    
                }                
            });
        }

        /// <summary>
        /// Expires queue messages being processed that have not been processed within the timeout. Message status is reset to Default
        /// so that it can be processed against.
        /// </summary>
        /// <param name="messageQueueId"></param>
        /// <returns></returns>
        private Task ExpiredProcessingMessagesAsync(string messageQueueId)
        {
            return Task.Run(async () =>
            {
                var isHasMutex = false;
                try
                {
                    _lastExpireProcessingMessages = DateTimeOffset.UtcNow;

                    // Get messages currently processing that have expired
                    var expiredQueueMessages = _queueMessageInternalProcessing.Where(m => m.MessageQueueId == messageQueueId)
                                                .Where(m => m.ProcessingStartDateTime.AddSeconds(m.MaxProcessingSeconds) <= DateTimeOffset.UtcNow).ToList();

                    if (expiredQueueMessages.Any())
                    {
                        using (var scope = _serviceProvider.CreateScope())
                        {
                            isHasMutex = _queueMutex.WaitOne();

                            var queueMessageService = scope.ServiceProvider.GetRequiredService<IQueueMessageInternalService>();

                            // Reset queue message status
                            while (expiredQueueMessages.Any())
                            {                                
                                var queueMessage = await queueMessageService.GetByIdAsync(expiredQueueMessages.First().Id);

                                if (queueMessage != null)
                                {
                                    queueMessage.Status = QueueMessageStatuses.Default;
                                    queueMessage.ProcessingStartDateTime = DateTimeOffset.MinValue;
                                    queueMessage.ProcessingMessageHubClientId = String.Empty;
                                    queueMessage.MaxProcessingSeconds = 0;

                                    await queueMessageService.UpdateAsync(queueMessage);
                                }

                                _queueMessageInternalProcessing.Remove(expiredQueueMessages.First());

                                expiredQueueMessages.RemoveAt(0);                                
                            }                            
                        }

                    }
                }
                finally
                {
                    if (isHasMutex) _queueMutex.ReleaseMutex();
                }
            });
        }

        /// <summary>
        /// Expires queue messages. Currently just deletes. Later then we made move them to an expired queue
        /// </summary>
        /// <returns></returns>
        private Task ExpireOldQueueMessagesAsync(string messageQueueId)
        {
            return Task.Run(async () =>
            {                
                var isHasMutex = false;
                try
                {
                    _lastExpireOldMessages = DateTimeOffset.UtcNow;

                    isHasMutex = _queueMutex.WaitOne();

                    using (var scope = _serviceProvider.CreateScope())
                    {
                        // Get services
                        var queueMessageService = scope.ServiceProvider.GetRequiredService<IQueueMessageInternalService>();

                        // Get expired messages
                        var expiredQueueMessages = await queueMessageService.GetExpiredAsync(messageQueueId, DateTimeOffset.UtcNow);

                        // Handle expired messages
                        while (expiredQueueMessages.Any())
                        {
                            var queueMessage = expiredQueueMessages.First();
                            expiredQueueMessages.Remove(queueMessage);

                            await queueMessageService.DeleteByIdAsync(queueMessage.Id);
                        }
                    }
                }
                finally
                {
                    if (isHasMutex) _queueMutex.ReleaseMutex();
                }
            });
        }        

        private MessageHubClient? GetMessageHubClientBySecurityKey(string securityKey, IMessageHubClientService messageHubClientService)
        {
            if (_messageHubClientsLastRefresh.AddMinutes(5) <= DateTimeOffset.UtcNow)       // Periodic refresh
            {
                _messageHubClientsLastRefresh = DateTimeOffset.UtcNow;
                _messageHubClientsBySecurityKey.Clear();
            }
            if (!_messageHubClientsBySecurityKey.Any())   // Cache empty, load it
            {
                var messageHubClients = messageHubClientService.GetAll();
                foreach (var messageHubClient in messageHubClients)
                {
                    _messageHubClientsBySecurityKey.TryAdd(messageHubClient.SecurityKey, messageHubClient);
                }
            }
            return _messageHubClientsBySecurityKey.ContainsKey(securityKey) ? _messageHubClientsBySecurityKey[securityKey] : null;
        }

        private void CheckCompleteQueueItemTasks(List<QueueItemTask> queueItemTasks)
        {
            // Get completed tasks
            var completedTasks = queueItemTasks.Where(t => t.Task.IsCompleted).ToList();

            // Process completed tasks
            while (completedTasks.Any())
            {
                var queueItemTask = completedTasks.First();
                completedTasks.Remove(queueItemTask);
                queueItemTasks.Remove(queueItemTask);

                ProcessCompletedQueueItemTask(queueItemTask);
            }
        }

        private void ProcessCompletedQueueItemTask(QueueItemTask queueItemTask)
        {
            if (queueItemTask.Task.Exception != null)                        
            {
                _log.Log(DateTimeOffset.UtcNow, "Error", $"Error processing task {queueItemTask.QueueItem.ItemType}: {queueItemTask.Task.Exception.Message}");
            }
        }

        private Task LogStatisticsTaskAsync(string messageQueueId)
        {
            return Task.Run(async () =>
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    _lastLogStatistics = DateTimeOffset.UtcNow;

                    var queueMessageService = scope.ServiceProvider.GetRequiredService<IQueueMessageInternalService>();

                    var messageCount = (await queueMessageService.GetByMessageQueueAsync(messageQueueId)).Count;                                        

                    _log.Log(DateTimeOffset.UtcNow, "Information", $"Statistics for queue {_messageQueue.Name}: Messages={messageCount}");
                }
            });
        }

        /// <summary>
        /// Notifies subscriptions of queue events        
        /// </summary>
        /// <returns></returns>
        private Task NotifySubscriptionsAsync()
        {
            return Task.Run(async () =>
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var queueMessageInternalService = scope.ServiceProvider.GetService<IQueueMessageInternalService>();

                    // Notify message added
                    foreach (var clientQueueSubscription in _clientQueueSubscriptions.Where(n => n.DoNotifyMessageAdded))
                    {                        
                        var messageQueueNotification = new MessageQueueNotificationMessage()
                        {
                            EventName = MessageQueueEventNames.MessageAdded
                        };

                        _clientsConnection.SendMessage(messageQueueNotification, clientQueueSubscription.RemoteEndpointInfo);

                        // Reset flag
                        clientQueueSubscription.DoNotifyMessageAdded = false;
                    }

                    // Notify queue cleared
                    foreach (var clientQueueSubscription in _clientQueueSubscriptions.Where(n => n.DoNotifyQueueCleared))
                    {                        
                        var messageQueueNotification = new MessageQueueNotificationMessage()
                        {
                            EventName = MessageQueueEventNames.QueueCleared
                        };

                        _clientsConnection.SendMessage(messageQueueNotification, clientQueueSubscription.RemoteEndpointInfo);

                        // Reset flag
                        clientQueueSubscription.DoNotifyQueueCleared = false;
                    }

                    // Notify queue size
                    long queueSize = -1;
                    foreach (var clientQueueSubscription in _clientQueueSubscriptions.Where(n => n.DoNotifyQueueSize))
                    {
                        if (queueSize == -1)
                        {
                            queueSize = (await queueMessageInternalService.GetByMessageQueueAsync(_messageQueue.Id)).Count;                                        
                        }
                        
                        var messageQueueNotification = new MessageQueueNotificationMessage()
                        {
                            EventName = MessageQueueEventNames.QueueSize,
                            QueueSize = queueSize
                        };

                        _clientsConnection.SendMessage(messageQueueNotification, clientQueueSubscription.RemoteEndpointInfo);

                        // Reset flag
                        clientQueueSubscription.LastNotifyQueueSize = DateTimeOffset.UtcNow;
                        clientQueueSubscription.DoNotifyQueueSize = false;
                    }
                }
            });        
        }
    }
}
