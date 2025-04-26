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

namespace CFMessageQueue.Hub
{
    public class MessageHubWorker
    {
        private readonly ConcurrentQueue<QueueItem> _queueItems = new();

        private readonly System.Timers.Timer _timer;

        private readonly List<QueueItemTask> _queueItemTasks = new List<QueueItemTask>();

        private readonly IServiceProvider _serviceProvider;

        private readonly ISimpleLog _log;
        private readonly IAuditLog _auditLog;
  
        private DateTimeOffset _lastArchiveLogsTime = DateTimeOffset.MinValue;

        private TimeSpan _logStatisticsFrequency = TimeSpan.FromSeconds(60);
        private DateTimeOffset _lastLogStatistics = DateTimeOffset.MinValue;

        private HubResources _hubResources;

        public MessageHubWorker(QueueMessageHub queueMessageHub, IServiceProvider serviceProvider,
                                 List<MessageQueueWorker> messageQueueWorkers, 
                                 SystemConfig  systemConfig)
        {            
            //_queueMessageHub = queueMessageHub;
            //_messageQueueWorkers = messageQueueWorkers;
            _serviceProvider = serviceProvider;
            //_systemConfig = systemConfig;
            _log = _serviceProvider.GetRequiredService<ISimpleLog>();
            _auditLog = _serviceProvider.GetRequiredService<IAuditLog>();

            _hubResources = new HubResources()
            {
                ClientsConnection = new MessageHubClientsConnection(serviceProvider),
                MessageHubClientsBySecurityKey = new Dictionary<string, MessageHubClient>(),
                MessageHubClientsLastRefresh = DateTimeOffset.MinValue,
                MessageQueueWorkers = messageQueueWorkers,
                QueueMessageHub = queueMessageHub,
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
                _log.Log(DateTimeOffset.UtcNow, "Information", $"Hub client connected {endpointInfo.Ip}:{endpointInfo.Port}");
            };

            // Handle client disconnection
            _hubResources.ClientsConnection.OnClientDisconnected += delegate (EndpointInfo endpointInfo)
            {
                _log.Log(DateTimeOffset.UtcNow, "Information", $"Hub client disconnected {endpointInfo.Ip}:{endpointInfo.Port}");
            };

            _timer = new System.Timers.Timer();
            _timer.Elapsed += _timer_Elapsed;
            _timer.Interval = 5000;
            _timer.Enabled = false;
        }

        public void Start()
        { 
            _log.Log(DateTimeOffset.UtcNow, "Information", "Worker starting");

            _timer.Enabled = true;

            _hubResources.ClientsConnection.StartListening(_hubResources.QueueMessageHub.Port);
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

                // Archive logs if time
                if (_lastArchiveLogsTime.AddHours(12) <= DateTimeOffset.UtcNow &&
                    !_queueItems.Any(i => i.ItemType == QueueItemTypes.ArchiveLogs))
                {
                    _queueItems.Enqueue(new QueueItem() { ItemType = QueueItemTypes.ArchiveLogs });
                }

                // Check if need to log statistics
                if (_lastLogStatistics.Add(_logStatisticsFrequency) <= DateTimeOffset.UtcNow &&
                    !_queueItems.Any(i => i.ItemType == QueueItemTypes.LogHubStatistics))
                {
                    _queueItems.Enqueue(new QueueItem() { ItemType = QueueItemTypes.LogHubStatistics });
                }

                ProcessQueueItems(() =>
                {
                    // Periodic action to do while processing queue items
                });

                CheckCompleteQueueItemTasks(_queueItemTasks);
            }
            catch (Exception exception)
            {
                _log.Log(DateTimeOffset.UtcNow, "Error", $"Error executing regular functions: {exception.Message}");
            }
            finally
            {
                _timer.Interval = _queueItems.Any() ||
                            _queueItemTasks.Any() ? 100 : 5000;
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
                switch(queueItem.Message.TypeId)
                {
                    case MessageTypeIds.AddMessageHubClientRequest:                        
                        _queueItemTasks.Add(new QueueItemTask(HandleAddMessageHubClientRequestAsync((AddMessageHubClientRequest)queueItem.Message, queueItem.MessageReceivedInfo), queueItem));
                        break;

                    case MessageTypeIds.AddMessageQueueRequest:                        
                        _queueItemTasks.Add(new QueueItemTask(HandleAddMessageQueueRequestAsync((AddMessageQueueRequest)queueItem.Message, queueItem.MessageReceivedInfo), queueItem));
                        break;

                    case MessageTypeIds.ConfigureMessageHubClientRequest:                        
                        _queueItemTasks.Add(new QueueItemTask(HandleConfigureMessageHubClientRequestAsync((ConfigureMessageHubClientRequest)queueItem.Message, queueItem.MessageReceivedInfo), queueItem));
                        break;

                    case MessageTypeIds.ExecuteMessageQueueActionRequest:                        
                        _queueItemTasks.Add(new QueueItemTask(HandleExecuteMessageQueueAction((ExecuteMessageQueueActionRequest)queueItem.Message, queueItem.MessageReceivedInfo), queueItem));
                        break;

                    case MessageTypeIds.GetMessageHubClientsRequest:                        
                        _queueItemTasks.Add(new QueueItemTask(HandleGetMessageHubClientsRequestAsync((GetMessageHubClientsRequest)queueItem.Message, queueItem.MessageReceivedInfo), queueItem));
                        break;

                    case MessageTypeIds.GetMessageHubsRequest:                        
                        _queueItemTasks.Add(new QueueItemTask(HandleGetMessageHubsRequestAsync((GetMessageHubsRequest)queueItem.Message, queueItem.MessageReceivedInfo), queueItem));
                        break;

                    case MessageTypeIds.GetMessageQueuesRequest:                        
                        _queueItemTasks.Add(new QueueItemTask(HandleGetMessageQueuesRequestAsync((GetMessageQueuesRequest)queueItem.Message, queueItem.MessageReceivedInfo), queueItem));
                        break;
                }
                */
            }
            else if (queueItem.ItemType == QueueItemTypes.ArchiveLogs)
            {
                _queueItemTasks.Add(new QueueItemTask(ArchiveLogsAsync(), queueItem));
            }
            else if (queueItem.ItemType == QueueItemTypes.LogHubStatistics)
            {
                _queueItemTasks.Add(new QueueItemTask(LogHubStatisticsAsync(), queueItem));
            }
        }

        /// <summary>
        /// Logs hub
        /// </summary>
        /// <returns></returns>
        private Task LogHubStatisticsAsync()
        {
            return Task.Run(async () =>
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    _lastLogStatistics = DateTimeOffset.UtcNow;

                    var messageQueueService = scope.ServiceProvider.GetRequiredService<IMessageQueueService>();                    

                    var messageQueues = await messageQueueService.GetAllAsync();


                    _log.Log(DateTimeOffset.UtcNow, "Information", $"Hub statistics: Message queues={messageQueues.Count}");                    
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
            if (queueItemTask.Task.Exception == null)
            {
                switch (queueItemTask.QueueItem.ItemType)
                {
                    case QueueItemTypes.ExternalMessage:
                        _log.Log(DateTimeOffset.UtcNow, "Information", $"Processed task {queueItemTask.QueueItem.Message.TypeId}");
                        break;
                    default:
                        _log.Log(DateTimeOffset.UtcNow, "Information", $"Processed task {queueItemTask.QueueItem.ItemType}");
                        break;
                }
            }
            else
            {
                _log.Log(DateTimeOffset.UtcNow, "Error", $"Error processing task {queueItemTask.QueueItem.ItemType}: {queueItemTask.Task.Exception.Message}");
            }
        }

        /// <summary>
        /// Archives logs
        /// </summary>
        private Task ArchiveLogsAsync()
        {
            return Task.Run(() =>
            {
                _log.Log(DateTimeOffset.UtcNow, "Information", "Archiving logs");

                DateTimeOffset date = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromDays(_hubResources.SystemConfig.MaxLogDays));

                _lastArchiveLogsTime = DateTimeOffset.UtcNow;

                for (int index = 0; index < 30; index++)
                {
                    // Delete simple log
                    var logFile = Path.Combine(_hubResources.SystemConfig.LogFolder, $"MessageQueueHub-Simple-{date.Subtract(TimeSpan.FromDays(index)).ToString("yyyy-MM-dd")}.txt");
                    if (File.Exists(logFile))
                    {
                        File.Delete(logFile);
                    }

                    // Delete audit log
                    logFile = Path.Combine(_hubResources.SystemConfig.LogFolder, $"MessageQueueHub-Audit-{date.Subtract(TimeSpan.FromDays(index)).ToString("yyyy-MM-dd")}.txt");
                    if (File.Exists(logFile))
                    {
                        File.Delete(logFile);
                    }
                }

                _log.Log(DateTimeOffset.UtcNow, "Information", "Archived logs");
            });
        }   
    }
}
