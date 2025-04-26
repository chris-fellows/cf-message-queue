using CFConnectionMessaging.Models;
using CFMessageQueue.Common.Interfaces;
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
        private readonly ConcurrentQueue<QueueItem> _queueItems = new();

        private readonly System.Timers.Timer _timer;   

        private readonly List<QueueItemTask> _queueItemTasks = new List<QueueItemTask>();        

        private readonly IServiceProvider _serviceProvider;
        
        private TimeSpan _expireOldMessagesFrequency = TimeSpan.FromSeconds(60);
        private DateTimeOffset _lastExpireOldMessages = DateTimeOffset.MinValue;

        private TimeSpan _expiredProcessingMessagesFrequency = TimeSpan.FromSeconds(60);
        private DateTimeOffset _lastExpireProcessingMessages = DateTimeOffset.MinValue;

        private TimeSpan _logStatisticsFrequency = TimeSpan.FromSeconds(60);
        private DateTimeOffset _lastLogStatistics = DateTimeOffset.MinValue;

        private readonly ISimpleLog _log;               

        private HubResources _hubResources;

        public MessageQueueWorker(MessageQueue messageQueue, IServiceProvider serviceProvider,SystemConfig systemConfig)
        {
            //_messageQueue = messageQueue;
            _serviceProvider = serviceProvider;
            _log = serviceProvider.GetRequiredService<ISimpleLog>();            

            _hubResources = new HubResources()
            {
                ClientsConnection = new MessageHubClientsConnection(serviceProvider),
                MessageQueue = messageQueue,
                QueueMutex = new Mutex(),
                MessageHubClientsBySecurityKey = new Dictionary<string, MessageHubClient>(),
                MessageHubClientsLastRefresh = DateTimeOffset.MinValue,
                SystemConfig = systemConfig
            };

            // Handle connection message received
            _hubResources.ClientsConnection.OnMessageReceived += delegate (MessageBase message, MessageReceivedInfo messageReceivedInfo)
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
            _hubResources.ClientsConnection.OnClientDisconnected += delegate (EndpointInfo endpointInfo)
            {
                _log.Log(DateTimeOffset.UtcNow, "Information", $"Queue client connected {endpointInfo.Ip}:{endpointInfo.Port}");
            };

            // Handle client disconnection
            _hubResources.ClientsConnection.OnClientDisconnected += delegate (EndpointInfo endpointInfo)
            {
                _log.Log(DateTimeOffset.UtcNow, "Information", $"Queue client disconnected {endpointInfo.Ip}:{endpointInfo.Port}");

                // Remove subscriptions
                _hubResources.ClientQueueSubscriptions.RemoveAll(s => s.RemoteEndpointInfo.Ip == endpointInfo.Ip && s.RemoteEndpointInfo.Port == endpointInfo.Port);
            };

            _timer = new System.Timers.Timer();
            _timer.Elapsed += _timer_Elapsed;
            _timer.Interval = 5000;
            _timer.Enabled = false;
        }

        public void NotifyQueueCleared()
        {
            _hubResources.ClientQueueSubscriptions.ForEach(c =>
            {
                c.DoNotifyQueueCleared = true;
                c.DoNotifyMessageAdded = false; // Sanity check
                c.NotifyIfMessageAdded = false; // Sanity check
            });
        }

        public string MessageQueueId => _hubResources.MessageQueue.Id;

        public Mutex QueueMutex => _hubResources.QueueMutex;
        
        public void Start()
        {
            _log.Log(DateTimeOffset.UtcNow, "Information", "Worker starting");

            _timer.Enabled = true;

            _hubResources.ClientsConnection.StartListening(_hubResources.MessageQueue.Port);
        }

        public void Stop()
        {
            _log.Log(DateTimeOffset.UtcNow, "Information", "Worker stopping");

            _timer.Enabled = false;

            _hubResources.ClientsConnection.StopListening();
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

                _hubResources.ClientQueueSubscriptions.Where(c => c.QueueSizeFrequencySecs > 0 &&
                                    c.LastNotifyQueueSize.AddSeconds(c.QueueSizeFrequencySecs) <= DateTimeOffset.UtcNow).ToList()
                                    .ForEach(s =>
                                    {
                                        s.DoNotifyQueueSize = true;
                                    });

                // Check if need to notify subscriptions
                if (_hubResources.ClientQueueSubscriptions.Any(s => s.IsNotificationRequired) &&
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
                            _hubResources.ClientQueueSubscriptions.Any(s => s.IsNotificationRequired) ? 100 : 5000;
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
                var messageProcessor = _serviceProvider.GetServices<IMessageProcessor>().FirstOrDefault(p => p.CanProcess(queueItem.Message));
                if (messageProcessor != null)
                {
                    messageProcessor.Configure(_hubResources);

                    _queueItemTasks.Add(new QueueItemTask(messageProcessor.ProcessAsync(queueItem.Message, queueItem.MessageReceivedInfo), queueItem));
                }

                /*
                switch (queueItem.Message.TypeId)
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
                */
            }
            else if (queueItem.ItemType == QueueItemTypes.ExpireOldQueueMessages)
            {
                _queueItemTasks.Add(new QueueItemTask(ExpireOldQueueMessagesAsync(_hubResources.MessageQueue.Id), queueItem));
            }
            else if (queueItem.ItemType == QueueItemTypes.ExpireProcessingQueueMessages)
            {
                _queueItemTasks.Add(new QueueItemTask(ExpiredProcessingMessagesAsync(_hubResources.MessageQueue.Id), queueItem));
            }
            else if (queueItem.ItemType == QueueItemTypes.LogQueueStatistics)
            {
                _queueItemTasks.Add(new QueueItemTask(LogStatisticsTaskAsync(_hubResources.MessageQueue.Id), queueItem));
            }
            else if (queueItem.ItemType == QueueItemTypes.QueueNotifications)
            {
                _queueItemTasks.Add(new QueueItemTask(NotifySubscriptionsAsync(), queueItem));
            }            
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
                    var expiredQueueMessages =  _hubResources.QueueMessageInternalProcessing.Where(m => m.MessageQueueId == messageQueueId)
                                                .Where(m => m.ProcessingStartDateTime.AddSeconds(m.MaxProcessingSeconds) <= DateTimeOffset.UtcNow).ToList();

                    if (expiredQueueMessages.Any())
                    {
                        using (var scope = _serviceProvider.CreateScope())
                        {
                            isHasMutex = _hubResources.QueueMutex.WaitOne();

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

                                _hubResources.QueueMessageInternalProcessing.Remove(expiredQueueMessages.First());

                                expiredQueueMessages.RemoveAt(0);                                
                            }                            
                        }

                    }
                }
                finally
                {
                    if (isHasMutex) _hubResources.QueueMutex.ReleaseMutex();
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

                    isHasMutex = _hubResources.QueueMutex.WaitOne();

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
                    if (isHasMutex) _hubResources.QueueMutex.ReleaseMutex();
                }
            });
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

                    _log.Log(DateTimeOffset.UtcNow, "Information", $"Statistics for queue {_hubResources.MessageQueue.Name}: Messages={messageCount}");
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
                    foreach (var clientQueueSubscription in _hubResources.ClientQueueSubscriptions.Where(n => n.DoNotifyMessageAdded))
                    {                        
                        var messageQueueNotification = new MessageQueueNotificationMessage()
                        {
                            EventName = MessageQueueEventNames.MessageAdded
                        };

                        _hubResources.ClientsConnection.SendMessage(messageQueueNotification, clientQueueSubscription.RemoteEndpointInfo);

                        // Reset flag
                        clientQueueSubscription.DoNotifyMessageAdded = false;
                    }

                    // Notify queue cleared
                    foreach (var clientQueueSubscription in _hubResources.ClientQueueSubscriptions.Where(n => n.DoNotifyQueueCleared))
                    {                        
                        var messageQueueNotification = new MessageQueueNotificationMessage()
                        {
                            EventName = MessageQueueEventNames.QueueCleared
                        };

                        _hubResources.ClientsConnection.SendMessage(messageQueueNotification, clientQueueSubscription.RemoteEndpointInfo);

                        // Reset flag
                        clientQueueSubscription.DoNotifyQueueCleared = false;
                    }

                    // Notify queue size
                    long queueSize = -1;
                    foreach (var clientQueueSubscription in _hubResources.ClientQueueSubscriptions.Where(n => n.DoNotifyQueueSize))
                    {
                        if (queueSize == -1)
                        {
                            queueSize = (await queueMessageInternalService.GetByMessageQueueAsync(_hubResources.MessageQueue.Id)).Count;                                        
                        }
                        
                        var messageQueueNotification = new MessageQueueNotificationMessage()
                        {
                            EventName = MessageQueueEventNames.QueueSize,
                            QueueSize = queueSize
                        };

                        _hubResources.ClientsConnection.SendMessage(messageQueueNotification, clientQueueSubscription.RemoteEndpointInfo);

                        // Reset flag
                        clientQueueSubscription.LastNotifyQueueSize = DateTimeOffset.UtcNow;
                        clientQueueSubscription.DoNotifyQueueSize = false;
                    }
                }
            });        
        }
    }
}
