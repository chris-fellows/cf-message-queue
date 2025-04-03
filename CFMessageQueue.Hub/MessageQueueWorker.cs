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
using System;
using System.Collections.Concurrent;

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

        private TimeSpan _expireMessagesFrequency = TimeSpan.FromSeconds(60);
        private DateTimeOffset _lastExpireQueueMessages = DateTimeOffset.MinValue;

        private readonly ISimpleLog _log;

        private readonly Mutex _queueMutex = new Mutex();

        /// <summary>
        /// Message hub clients subscribed for this queue.
        /// 
        /// It will mainly be used for notifying the client that new messages are in the queue.
        /// We don't really know how the client intends to interact with us.
        /// 
        /// We don't really want to generate lots of network traffic by notifying the client of every message
        /// that is added.
        /// 
        /// How the client may interact with hub:
        /// ------------------------------------
        /// - Client calls GetNextAsync() and if it returns nothing then it waits for a 'Message added' notification
        ///   and calls GetNextAsync() until it returns nothing.
        ///   (This is the easiest scenario to handle).
        /// 
        /// - Client waits for a 'Message added' notification and then calls GetNextAsync(). We don't know if the client
        ///   will call GetNextAsync() again.
        /// 
        /// Notifications:
        /// - Message added.
        /// - Queue cleared.
        /// </summary>

        /// <summary>
        /// Client subscription info. Indicates which notifications are required
        /// </summary>
        private class ClientSubscriptionInfo
        {
            public string MessageHubClientId { get; set; } = String.Empty;

            /// <summary>
            /// Frequency to notify queue size (0=Never)
            /// </summary>
            public long QueueSizeFrequencySecs { get; set; }

            public DateTimeOffset LastNotifyQueueSize { get; set; } = DateTimeOffset.UtcNow;

            /// <summary>
            /// Notify client of queue size
            /// </summary>
            public bool DoNotifyQueueSize { get; set; }

            /// <summary>
            /// Whether to notify client if a new message is added.
            /// </summary>
            public bool NotifyIfMessageAdded { get; set; }

            /// <summary>
            /// Notify client that message was added
            /// </summary>
            public bool DoNotifyMessageAdded { get; set; }

            /// <summary>
            /// Notifyt client that queue was cleared
            /// </summary>
            public bool DoNotifyQueueCleared { get; set; }

            public bool IsNotificationRequired => DoNotifyQueueSize || DoNotifyMessageAdded || DoNotifyQueueCleared;
        }

        private readonly List<ClientSubscriptionInfo> _clientSubscriptionInfos = new();

        public MessageQueueWorker(MessageQueue messageQueue, IServiceProvider serviceProvider)
        {
            _messageQueue = messageQueue;
            _serviceProvider = serviceProvider;
            _log = serviceProvider.GetRequiredService<ISimpleLog>();

            _messageQueueClientsConnection = new MessageQueueClientsConnection(serviceProvider);            
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

            // Load subscribed clients
            var messageQueueSubscriptionService = _serviceProvider.GetRequiredService<IMessageQueueSubscriptionService>();
            var messageQueueSubscriptions = messageQueueSubscriptionService.GetAll()
                                        .Where(s => s.MessageQueueId == _messageQueue.Id).ToList();

            // 
            _clientSubscriptionInfos.AddRange(messageQueueSubscriptions.Select(s =>
            {
                return new ClientSubscriptionInfo
                {
                    MessageHubClientId = s.MessageHubClientId,
                    NotifyIfMessageAdded = true,     // Notify when first message added
                    DoNotifyMessageAdded = false
                };
            }).ToList());

            _timer = new System.Timers.Timer();
            _timer.Elapsed += _timer_Elapsed;
            _timer.Interval = 5000;
            _timer.Enabled = false;
        }

        public void NotifyQueueCleared()
        {
            _clientSubscriptionInfos.ForEach(c =>
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
                if (_lastExpireQueueMessages.Add(_expireMessagesFrequency) <= DateTimeOffset.UtcNow &&
                    !_queueItems.Any(q => q.ItemType == QueueItemTypes.ExpireQueueMessages)) 
                {
                    _queueItems.Append(new QueueItem() { ItemType = QueueItemTypes.ExpireQueueMessages });
                }                

                _clientSubscriptionInfos.Where(c => c.QueueSizeFrequencySecs > 0 &&
                                    c.LastNotifyQueueSize.AddSeconds(c.QueueSizeFrequencySecs) <= DateTimeOffset.UtcNow).ToList()
                                    .ForEach(s =>
                                    {
                                        s.DoNotifyQueueSize = true;
                                    });

                // Check if need to notify subscriptions
                if (_clientSubscriptionInfos.Any(s => s.IsNotificationRequired) &&
                    !_queueItems.Any(q => q.ItemType == QueueItemTypes.QueueNotifications))
                {
                    _queueItems.Append(new QueueItem() { ItemType = QueueItemTypes.QueueNotifications });
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
                            _clientSubscriptionInfos.Any(s => s.IsNotificationRequired) ? 100 : 5000;
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

                    case MessageTypeIds.MessageQueueSubscribeRequest:
                        var messageQueueSubscribeRequest = _messageQueueClientsConnection.MessageConverterList.MessageQueueSubscribeRequestConverter.GetExternalMessage(queueItem.ConnectionMessage);
                        _queueItemTasks.Add(new QueueItemTask(HandleMessageQueueSubscribeRequestAsync(messageQueueSubscribeRequest, queueItem.MessageReceivedInfo), queueItem));
                        break;
                }
            }
            else if (queueItem.ItemType == QueueItemTypes.ExpireQueueMessages)
            {
                _queueItemTasks.Add(new QueueItemTask(ExpireQueueMessagesAsync(_messageQueue.Id), queueItem));
            }
            else if (queueItem.ItemType == QueueItemTypes.QueueNotifications)
            {
                _queueItemTasks.Add(new QueueItemTask(NotifySubscriptionsAsync(), queueItem));
            }
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

                            // Set message queue
                            addQueueMessageRequest.QueueMessage.MessageQueueId = _messageQueue.Id;

                            // Set the sender
                            addQueueMessageRequest.QueueMessage.SenderMessageHubClientId = messageHubClient.Id;

                            await queueMessageService.AddAsync(addQueueMessageRequest.QueueMessage);

                            // If client subscription indicates waiting for new message then flag notification
                            _clientSubscriptionInfos.Where(s => s.NotifyIfMessageAdded).ToList()
                                    .ForEach(s =>
                                    {
                                        s.DoNotifyMessageAdded = true;
                                        s.NotifyIfMessageAdded = false;
                                    });                                   
                        }

                        // Send response
                        _messageQueueClientsConnection.SendAddQueueMessageResponse(response, messageReceivedInfo);
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

                            // TODO: Make this more efficient
                            var queueMessage = (await queueMessageService.GetAllAsync())
                                                .Where(m => m.MessageQueueId == messageQueue.Id)
                                                .Where(m => m.ExpirySeconds == 0 || m.CreatedDateTime.AddSeconds(m.ExpirySeconds) < DateTimeOffset.UtcNow)  // not expired
                                                .OrderBy(m => m.CreatedDateTime).FirstOrDefault();

                            response.QueueMessage = queueMessage;

                            // If there's no message then we should notify subscriptions when new message is added
                            if (queueMessage == null)
                            {
                                _clientSubscriptionInfos.ForEach(s =>
                                {
                                    s.NotifyIfMessageAdded = true;
                                    s.DoNotifyMessageAdded = false;
                                });
                            }
                        }

                        // Send response
                        _messageQueueClientsConnection.SendGetNextQueueMessageResponse(response, messageReceivedInfo);
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
                    var messageQueueSubscriptionService = scope.ServiceProvider.GetRequiredService<IMessageQueueSubscriptionService>();
                  
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
                    else if (messageQueueSubscribeRequest.QueueSizeFrequencySecs > 0 &&
                            messageQueueSubscribeRequest.QueueSizeFrequencySecs < 30)       // Limit smallest duration
                    {
                        response.Response.ErrorCode = ResponseErrorCodes.InvalidParameters;
                        response.Response.ErrorMessage = $"{EnumUtilities.GetEnumDescription(response.Response.ErrorCode)}: Queue Frequency Size Seconds must be 30 seconds or more ";
                    }
                    else
                    {
                        // Check if subscription exists for client & queue
                        var messageQueueSubscription = (await messageQueueSubscriptionService.GetAllAsync())
                                            .Where(s => s.MessageQueueId == messageQueueSubscribeRequest.MessageQueueId &&
                                                    s.MessageHubClientId == messageHubClient.Id).FirstOrDefault();


                        switch (messageQueueSubscribeRequest.ActionName)
                        {
                            case "SUBSCRIBE":
                                if (messageQueueSubscription == null)   // Not subscribed
                                {
                                    messageQueueSubscription = new MessageQueueSubscription()
                                    {
                                        Id = Guid.NewGuid().ToString(),
                                        MessageHubClientId = messageHubClient.Id,
                                        MessageQueueId = messageQueueSubscribeRequest.MessageQueueId
                                    };

                                    await messageQueueSubscriptionService.AddAsync(messageQueueSubscription);
                                }

                                // Add client subscription
                                var clientSubscriptionInfo = _clientSubscriptionInfos.FirstOrDefault(s => s.MessageHubClientId == messageHubClient.Id);
                                if (clientSubscriptionInfo == null)
                                {
                                    clientSubscriptionInfo = new ClientSubscriptionInfo()
                                    {
                                        MessageHubClientId = messageHubClient.Id,
                                        QueueSizeFrequencySecs = messageQueueSubscribeRequest.QueueSizeFrequencySecs,
                                        LastNotifyQueueSize = DateTimeOffset.MinValue   // Send queue size ASAP
                                    };
                                    _clientSubscriptionInfos.Add(clientSubscriptionInfo);
                                }

                                response.SubscribeId = messageQueueSubscription.Id;                                
                                break;

                            case "UNSUBSCRIBE":
                                if (messageQueueSubscription != null)
                                {
                                    await messageQueueSubscriptionService.DeleteByIdAsync(messageQueueSubscription.Id);
                                }

                                _clientSubscriptionInfos.RemoveAll(s => s.MessageHubClientId == messageHubClient.Id);                                
                                break;

                            default:
                                response.Response.ErrorCode = ResponseErrorCodes.InvalidParameters;
                                response.Response.ErrorMessage = $"{EnumUtilities.GetEnumDescription(response.Response.ErrorCode)}: Action Name {messageQueueSubscribeRequest.ActionName} is invalid";
                                break;
                        }
                    }

                    // Send response
                    _messageQueueClientsConnection.SendMessageQueueSubscribeResponse(response, messageReceivedInfo);
                }

                _log.Log(DateTimeOffset.UtcNow, "Information", $"Processed {messageQueueSubscribeRequest.TypeId}");
            });
        }

        /// <summary>
        /// Expires queue messages. Currently just deletes. Later then we made move them to an expired queue
        /// </summary>
        /// <returns></returns>
        private Task ExpireQueueMessagesAsync(string messageQueueId)
        {
            return Task.Factory.StartNew(async () =>
            {                
                var isHasMutex = false;
                try
                {
                    _lastExpireQueueMessages = DateTimeOffset.UtcNow;

                    isHasMutex = _queueMutex.WaitOne();

                    using (var scope = _serviceProvider.CreateScope())
                    {
                        // Get services
                        var queueMessageService = scope.ServiceProvider.GetRequiredService<IQueueMessageInternalService>();

                        // Get expired messages
                        var queueMessages = await queueMessageService.GetExpired(messageQueueId, DateTimeOffset.UtcNow);

                        // Handle expired messages
                        while (queueMessages.Any())
                        {
                            var queueMessage = queueMessages.First();
                            queueMessages.Remove(queueMessage);

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
                    foreach (var clientNotificationInfo in _clientSubscriptionInfos.Where(n => n.DoNotifyMessageAdded))
                    {
                        // TODO: How to work out which TCP connection belongs to the client

                        var messageQueueNotification = new MessageQueueNotificationMessage()
                        {
                            EventName = MessageQueueEventNames.MessageAdded
                        };

                        // Reset flag
                        clientNotificationInfo.DoNotifyMessageAdded = false;
                    }

                    // Notify queue cleared
                    foreach (var clientNotificationInfo in _clientSubscriptionInfos.Where(n => n.DoNotifyQueueCleared))
                    {
                        // TODO: How to work out which TCP connection belongs to the client

                        var messageQueueNotification = new MessageQueueNotificationMessage()
                        {
                            EventName = MessageQueueEventNames.QueueCleared
                        };

                        // Reset flag
                        clientNotificationInfo.DoNotifyQueueCleared = false;
                    }

                    // Notify queue size
                    long queueSize = -1;
                    foreach (var clientNotificationInfo in _clientSubscriptionInfos.Where(n => n.DoNotifyQueueSize))
                    {
                        if (queueSize == -1)
                        {
                            queueSize = (await queueMessageInternalService.GetAllAsync())
                                        .Where(m => m.MessageQueueId == _messageQueue.Id).Count();

                        }

                        // TODO: How to work out which TCP connection belongs to the client

                        var messageQueueNotification = new MessageQueueNotificationMessage()
                        {
                            EventName = MessageQueueEventNames.QueueSize,
                            QueueSize = queueSize
                        };

                        // Reset flag
                        clientNotificationInfo.LastNotifyQueueSize = DateTimeOffset.UtcNow;
                        clientNotificationInfo.DoNotifyQueueSize = false;
                    }
                }
            });        
        }
    }
}
