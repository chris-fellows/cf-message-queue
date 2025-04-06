using CFConnectionMessaging.Models;
using CFMessageQueue.Constants;
using CFMessageQueue.Enums;
using CFMessageQueue.Hub.Enums;
using CFMessageQueue.Hub.Models;
using CFMessageQueue.Interfaces;
using CFMessageQueue.Logs;
using CFMessageQueue.Models;
using CFMessageQueue.Utilities;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;

namespace CFMessageQueue.Hub
{
    /// <summary>
    /// Worker that handles single message queue.
    /// </summary>
    public class MessageQueueWorker
    {
        private readonly MessageQueueClientsConnection _messageQueueClientsConnection;

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

            _messageQueueClientsConnection = new MessageQueueClientsConnection(serviceProvider);            

            // Handle connection message received
            _messageQueueClientsConnection.OnConnectionMessageReceived += delegate (ConnectionMessage connectionMessage, MessageReceivedInfo messageReceivedInfo)
            {
                _log.Log(DateTimeOffset.UtcNow, "Information", $"Received message {connectionMessage.TypeId} from {messageReceivedInfo.RemoteEndpointInfo.Ip}:{messageReceivedInfo.RemoteEndpointInfo.Port}");

                var queueItem = new QueueItem()
                {
                    ItemType = QueueItemTypes.ConnectionMessage,
                    ConnectionMessage = connectionMessage,
                    MessageReceivedInfo = messageReceivedInfo
                };
                _queueItems.Enqueue(queueItem);

                _timer.Interval = 100;
            };

            // Handle client connection
            _messageQueueClientsConnection.OnClientDisconnected += delegate (EndpointInfo endpointInfo)
            {
                _log.Log(DateTimeOffset.UtcNow, "Information", $"Queue client connected {endpointInfo.Ip}:{endpointInfo.Port}");
            };

            // Handle client disconnection
            _messageQueueClientsConnection.OnClientDisconnected += delegate (EndpointInfo endpointInfo)
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

            _messageQueueClientsConnection.StartListening(_messageQueue.Port);
        }

        public void Stop()
        {
            _log.Log(DateTimeOffset.UtcNow, "Information", "Worker stopping");

            _timer.Enabled = false;

            _messageQueueClientsConnection.StopListening();
        }

        private void _timer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                _timer.Enabled = false;

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
            if (queueItem.ItemType == QueueItemTypes.ConnectionMessage && queueItem.ConnectionMessage != null)
            {
                switch(queueItem.ConnectionMessage.TypeId)
                {
                    case MessageTypeIds.AddQueueMessageRequest:
                        var addQueueMessageRequest = _messageQueueClientsConnection.MessageConverterList.AddQueueMessageRequestConverter.GetExternalMessage(queueItem.ConnectionMessage);
                        _queueItemTasks.Add(new QueueItemTask(HandleAddQueueMessageRequestAsync(addQueueMessageRequest, queueItem.MessageReceivedInfo), queueItem));
                        break;

                    case MessageTypeIds.GetNextQueueMessageRequest:
                        var getNextQueueMessageRequest = _messageQueueClientsConnection.MessageConverterList.GetNextQueueMessageRequestConverter.GetExternalMessage(queueItem.ConnectionMessage);
                        _queueItemTasks.Add(new QueueItemTask(HandleGetNextQueueMessageRequestAsync(getNextQueueMessageRequest, queueItem.MessageReceivedInfo), queueItem));
                        break;

                    case MessageTypeIds.GetQueueMessagesRequest:
                        var getQueueMessagesRequest = _messageQueueClientsConnection.MessageConverterList.GetQueueMessagesRequestConverter.GetExternalMessage(queueItem.ConnectionMessage);
                        _queueItemTasks.Add(new QueueItemTask(HandleGetQueueMessagesRequestAsync(getQueueMessagesRequest, queueItem.MessageReceivedInfo), queueItem));
                        break;

                    case MessageTypeIds.MessageQueueSubscribeRequest:
                        var messageQueueSubscribeRequest = _messageQueueClientsConnection.MessageConverterList.MessageQueueSubscribeRequestConverter.GetExternalMessage(queueItem.ConnectionMessage);
                        _queueItemTasks.Add(new QueueItemTask(HandleMessageQueueSubscribeRequestAsync(messageQueueSubscribeRequest, queueItem.MessageReceivedInfo), queueItem));
                        break;

                    case MessageTypeIds.QueueMessageProcessedMessage:
                        var queueMessageProcessedMessage = _messageQueueClientsConnection.MessageConverterList.QueueMessageProcessedMessageConverter.GetExternalMessage(queueItem.ConnectionMessage);
                        _queueItemTasks.Add(new QueueItemTask(HandleQueueMessageProcessedMessageAsync(queueMessageProcessedMessage, queueItem.MessageReceivedInfo), queueItem));
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

        private Task HandleQueueMessageProcessedMessageAsync(QueueMessageProcessedMessage queueMessageProcessedMessage, MessageReceivedInfo messageReceivedInfo)
        {
            return Task.Factory.StartNew(async () =>
            {
                var isHasMutex = false;

                _log.Log(DateTimeOffset.UtcNow, "Information", $"Processing {queueMessageProcessedMessage.TypeId}");

                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var messageHubClientService = scope.ServiceProvider.GetRequiredService<IMessageHubClientService>();
                        var messageQueueService = scope.ServiceProvider.GetRequiredService<IMessageQueueService>();
                        var queueMessageService = scope.ServiceProvider.GetRequiredService<IQueueMessageInternalService>();

                        var response = new AddQueueMessageResponse()
                        {
                            Response = new MessageResponse()
                            {
                                IsMore = false,
                                MessageId = queueMessageProcessedMessage.Id,
                                Sequence = 1
                            },
                        };

                        var messageHubClient = GetMessageHubClientBySecurityKey(queueMessageProcessedMessage.SecurityKey, messageHubClientService);

                        var messageQueue = await messageQueueService.GetByIdAsync(queueMessageProcessedMessage.MessageQueueId);

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
                            var queueMessageMemory = _queueMessageInternalProcessing.FirstOrDefault(m => m.Id == queueMessageProcessedMessage.QueueMessageId);
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
                                if (queueMessageProcessedMessage.Processed)
                                {
                                    // Delete queue message
                                    await queueMessageService.DeleteByIdAsync(queueMessageProcessedMessage.QueueMessageId);
                                }
                                else
                                {
                                    // Reset queue message status
                                    var queueMessage = await queueMessageService.GetByIdAsync(queueMessageProcessedMessage.QueueMessageId);

                                    queueMessage.Status = QueueMessageStatuses.Default;
                                    queueMessage.ProcessingMessageHubClientId = "";
                                    queueMessage.ProcessingStartDateTime = DateTimeOffset.MinValue;
                                    queueMessage.MaxProcessingMilliseconds = 0;

                                    await queueMessageService.UpdateAsync(queueMessage);
                                }

                               _queueMessageInternalProcessing.Remove(queueMessageMemory);
                            }
                        }
                    }
                }
                finally
                {
                    if (isHasMutex) _queueMutex.ReleaseMutex();
                }

                _log.Log(DateTimeOffset.UtcNow, "Information", $"Processed {queueMessageProcessedMessage.TypeId}");
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
            return Task.Factory.StartNew(async () =>
            {
                var isHasMutex = false;

                _log.Log(DateTimeOffset.UtcNow, "Information", $"Processing {addQueueMessageRequest.TypeId}");

                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var messageHubClientService = scope.ServiceProvider.GetRequiredService<IMessageHubClientService>();
                        var messageQueueService = scope.ServiceProvider.GetRequiredService<IMessageQueueService>();
                        var queueMessageService = scope.ServiceProvider.GetRequiredService<IQueueMessageInternalService>();

                        var response = new AddQueueMessageResponse()
                        {
                            Response = new MessageResponse()
                            {
                                IsMore = false,
                                MessageId = addQueueMessageRequest.Id,
                                Sequence = 1
                            },
                        };

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
                        else
                        {
                            isHasMutex = _queueMutex.WaitOne();

                            // Check if queue is full
                            // TODO: Make this more efficient
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

                                // Set message queue
                                addQueueMessageRequest.QueueMessage.MessageQueueId = _messageQueue.Id;

                                // Set the sender
                                addQueueMessageRequest.QueueMessage.SenderMessageHubClientId = messageHubClient.Id;

                                await queueMessageService.AddAsync(addQueueMessageRequest.QueueMessage);

                                // If client subscription indicates waiting for new message then flag notification
                                _clientQueueSubscriptions.Where(s => s.NotifyIfMessageAdded).ToList()
                                        .ForEach(s =>
                                        {
                                            s.DoNotifyMessageAdded = true;
                                            s.NotifyIfMessageAdded = false;
                                        });
                            }
                        }

                        // Send response
                        _messageQueueClientsConnection.SendAddQueueMessageResponse(response, messageReceivedInfo.RemoteEndpointInfo);
                    }
                }
                finally
                {
                    if (isHasMutex) _queueMutex.ReleaseMutex();
                }

                _log.Log(DateTimeOffset.UtcNow, "Information", $"Processed {addQueueMessageRequest.TypeId}");
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
            return Task.Factory.StartNew(async () =>
            {
                _log.Log(DateTimeOffset.UtcNow, "Information", $"Processing {getNextQueueMessageRequest.TypeId}");

                var isHasMutex = false;
                try
                {                    
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var messageHubClientService = scope.ServiceProvider.GetRequiredService<IMessageHubClientService>();
                        var messageQueueService = scope.ServiceProvider.GetRequiredService<IMessageQueueService>();
                        var queueMessageService = scope.ServiceProvider.GetRequiredService<IQueueMessageInternalService>();

                        var response = new GetNextQueueMessageResponse()
                        {
                            Response = new MessageResponse()
                            {
                                IsMore = false,
                                MessageId = getNextQueueMessageRequest.Id,
                                Sequence = 1
                            },
                        };

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
                        else
                        {
                            // Try and get message, if no message then may need to wait for message
                            bool waitedForMessage = false;
                            QueueMessageInternal? queueMessage = null;
                            var stopwatch = new Stopwatch();    // Started on first
                            while (!waitedForMessage)
                            {
                                // Get mutex. Need to hold it for shortest time possible
                                isHasMutex = _queueMutex.WaitOne();

                                // Get next message. Only allowed if not exceeded max number of concurrent messages being processed                             
                                if (messageQueue.MaxConcurrentProcessing == 0 ||
                                    _queueMessageInternalProcessing.Count < messageQueue.MaxConcurrentProcessing)
                                {
                                    // TODO: Make this more efficient
                                    queueMessage = (await queueMessageService.GetAllAsync())
                                                    .Where(m => m.MessageQueueId == messageQueue.Id)
                                                    .Where(m => m.Status == QueueMessageStatuses.Default)
                                                    .Where(m => m.ExpirySeconds == 0 || m.CreatedDateTime.AddSeconds(m.ExpirySeconds) < DateTimeOffset.UtcNow)  // Not expired
                                                    .OrderBy(m => m.CreatedDateTime).FirstOrDefault();
                                }
                                                                
                                if (queueMessage != null)
                                {
                                    // Update message as Processing
                                    queueMessage.Status = QueueMessageStatuses.Processing;
                                    queueMessage.ProcessingMessageHubClientId = messageHubClient.Id;
                                    queueMessage.MaxProcessingMilliseconds = getNextQueueMessageRequest.MaxProcessingMilliseconds;
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
                                                Thread.Sleep(200);
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

                        // Send response
                        _messageQueueClientsConnection.SendGetNextQueueMessageResponse(response, messageReceivedInfo.RemoteEndpointInfo);
                    }
                }
                finally
                {
                    if (isHasMutex) _queueMutex.ReleaseMutex();
                }

                _log.Log(DateTimeOffset.UtcNow, "Information", $"Processed {getNextQueueMessageRequest.TypeId}");
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
            return Task.Factory.StartNew(async () =>
            {
                _log.Log(DateTimeOffset.UtcNow, "Information", $"Processing {getQueueMessagesRequest.TypeId}");

                var isHasMutex = false;
                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var messageHubClientService = scope.ServiceProvider.GetRequiredService<IMessageHubClientService>();
                        var messageQueueService = scope.ServiceProvider.GetRequiredService<IMessageQueueService>();
                        var queueMessageService = scope.ServiceProvider.GetRequiredService<IQueueMessageInternalService>();

                        var response = new GetQueueMessagesResponse()
                        {
                            Response = new MessageResponse()
                            {
                                IsMore = false,
                                MessageId = getQueueMessagesRequest.Id,
                                Sequence = 1
                            },
                        };

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

                        // Send response
                        _messageQueueClientsConnection.SendGetQueueMessagesResponse(response, messageReceivedInfo.RemoteEndpointInfo);
                    }
                }
                finally
                {
                    if (isHasMutex) _queueMutex.ReleaseMutex();
                }

                _log.Log(DateTimeOffset.UtcNow, "Information", $"Processed {getQueueMessagesRequest.TypeId}");
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
            return Task.Factory.StartNew(async () =>
            {
                _log.Log(DateTimeOffset.UtcNow, "Information", $"Processing {messageQueueSubscribeRequest.TypeId}");

                using (var scope = _serviceProvider.CreateScope())
                {
                    var messageHubClientService = scope.ServiceProvider.GetRequiredService<IMessageHubClientService>();
                    var messageQueueService = scope.ServiceProvider.GetRequiredService<IMessageQueueService>();
                    var queueMessageService = scope.ServiceProvider.GetRequiredService<IQueueMessageInternalService>();
                    //var messageQueueSubscriptionService = scope.ServiceProvider.GetRequiredService<IMessageQueueSubscriptionService>();
                  
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

                    // Send response
                    _messageQueueClientsConnection.SendMessageQueueSubscribeResponse(response, messageReceivedInfo.RemoteEndpointInfo);
                }

                _log.Log(DateTimeOffset.UtcNow, "Information", $"Processed {messageQueueSubscribeRequest.TypeId}");
            });
        }

        /// <summary>
        /// Expires queue messages being processed that have not been processed within the timeout
        /// </summary>
        /// <param name="messageQueueId"></param>
        /// <returns></returns>
        private Task ExpiredProcessingMessagesAsync(string messageQueueId)
        {
            return Task.Factory.StartNew(async () =>
            {
                var isHasMutex = false;
                try
                {
                    _lastExpireProcessingMessages = DateTimeOffset.UtcNow;

                    var expiredQueueMessages = _queueMessageInternalProcessing.Where(m => m.MessageQueueId == messageQueueId)
                                                .Where(m => m.ProcessingStartDateTime.AddMilliseconds(m.MaxProcessingMilliseconds) < DateTimeOffset.UtcNow).ToList();

                    if (expiredQueueMessages.Any())
                    {
                        using (var scope = _serviceProvider.CreateScope())
                        {
                            isHasMutex = _queueMutex.WaitOne();

                            var queueMessageService = scope.ServiceProvider.GetRequiredService<IQueueMessageInternalService>();

                            while (expiredQueueMessages.Any())
                            {
                                // Reset queue message status
                                var queueMessage = await queueMessageService.GetByIdAsync(expiredQueueMessages.First().Id);                                

                                queueMessage.Status = QueueMessageStatuses.Default;
                                queueMessage.ProcessingStartDateTime = DateTimeOffset.MinValue;
                                queueMessage.ProcessingMessageHubClientId = String.Empty;
                                queueMessage.MaxProcessingMilliseconds = 0;

                                await queueMessageService.UpdateAsync(queueMessage);

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
            return Task.Factory.StartNew(async () =>
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
            if (queueItemTask.Task.Exception == null)
            {
                Console.WriteLine($"Processing task {queueItemTask.QueueItem.ItemType}");
            }
            else
            {
                Console.WriteLine($"Error processing task {queueItemTask.QueueItem.ItemType}: {queueItemTask.Task.Exception.Message}");
            }
        }

        private Task LogStatisticsTaskAsync(string messageQueueId)
        {
            return Task.Factory.StartNew(async () =>
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
            return Task.Factory.StartNew(async () =>
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

                        _messageQueueClientsConnection.SendMessageQueueNotificationMessage(messageQueueNotification, clientQueueSubscription.RemoteEndpointInfo);

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

                        _messageQueueClientsConnection.SendMessageQueueNotificationMessage(messageQueueNotification, clientQueueSubscription.RemoteEndpointInfo);

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

                        _messageQueueClientsConnection.SendMessageQueueNotificationMessage(messageQueueNotification, clientQueueSubscription.RemoteEndpointInfo);

                        // Reset flag
                        clientQueueSubscription.LastNotifyQueueSize = DateTimeOffset.UtcNow;
                        clientQueueSubscription.DoNotifyQueueSize = false;
                    }
                }
            });        
        }
    }
}
