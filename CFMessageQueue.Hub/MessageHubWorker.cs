﻿using CFConnectionMessaging.Models;
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
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CFMessageQueue.Hub
{
    public class MessageHubWorker
    {
        private readonly MessageHubClientsConnection _messageHubClientsConnection;

        private readonly ConcurrentQueue<QueueItem> _queueItems = new();

        private readonly System.Timers.Timer _timer;

        private QueueMessageHub _queueMessageHub;

        private readonly List<QueueItemTask> _queueItemTasks = new List<QueueItemTask>();

        private readonly List<MessageQueueWorker> _messageQueueWorkers;

        private readonly IServiceProvider _serviceProvider;

        private DateTimeOffset _messageHubClientsLastRefresh = DateTimeOffset.MinValue;
        private readonly Dictionary<string, MessageHubClient> _messageHubClientsBySecurityKey = new();

        private readonly SystemConfig _systemConfig;

        private readonly ISimpleLog _log;

        //private readonly Mutex _hubMutex = new Mutex();     // For hub level 

        private DateTimeOffset _lastArchiveLogsTime = DateTimeOffset.MinValue;

        private TimeSpan _logStatisticsFrequency = TimeSpan.FromSeconds(60);
        private DateTimeOffset _lastLogStatistics = DateTimeOffset.MinValue;

        public MessageHubWorker(QueueMessageHub queueMessageHub, IServiceProvider serviceProvider,
                                 List<MessageQueueWorker> messageQueueWorkers, 
                                 SystemConfig  systemConfig)
        {            
            _queueMessageHub = queueMessageHub;
            _messageQueueWorkers = messageQueueWorkers;
            _serviceProvider = serviceProvider;
            _systemConfig = systemConfig;
            _log = _serviceProvider.GetRequiredService<ISimpleLog>();

            _messageHubClientsConnection = new MessageHubClientsConnection(serviceProvider);

            // Handle connection message received
            _messageHubClientsConnection.OnConnectionMessageReceived += delegate (ConnectionMessage connectionMessage, MessageReceivedInfo messageReceivedInfo)
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
            _messageHubClientsConnection.OnClientDisconnected += delegate (EndpointInfo endpointInfo)
            {
                _log.Log(DateTimeOffset.UtcNow, "Information", $"Hub client connected {endpointInfo.Ip}:{endpointInfo.Port}");
            };

            // Handle client disconnection
            _messageHubClientsConnection.OnClientDisconnected += delegate (EndpointInfo endpointInfo)
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

            _messageHubClientsConnection.StartListening(_queueMessageHub.Port);
        }

        public void Stop()
        {
             _log.Log(DateTimeOffset.UtcNow, "Information", "Worker stopping");

            _timer.Enabled = false;

            _messageHubClientsConnection.StopListening();
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
            if (queueItem.ItemType == QueueItemTypes.ConnectionMessage && queueItem.ConnectionMessage != null)
            {
                switch(queueItem.ConnectionMessage.TypeId)
                {
                    case MessageTypeIds.AddMessageHubClientRequest:
                        var addMessageHubClientRequest = _messageHubClientsConnection.MessageConverterList.AddMessageHubClientRequestConverter.GetExternalMessage(queueItem.ConnectionMessage);
                        _queueItemTasks.Add(new QueueItemTask(HandleAddMessageHubClientRequestAsync(addMessageHubClientRequest, queueItem.MessageReceivedInfo), queueItem));
                        break;

                    case MessageTypeIds.AddMessageQueueRequest:
                        var addMessageQueueRequest = _messageHubClientsConnection.MessageConverterList.AddMessageQueueRequestConverter.GetExternalMessage(queueItem.ConnectionMessage);
                        _queueItemTasks.Add(new QueueItemTask(HandleAddMessageQueueRequestAsync(addMessageQueueRequest, queueItem.MessageReceivedInfo), queueItem));
                        break;

                    case MessageTypeIds.ConfigureMessageHubClientRequest:
                        var configureMessageHubClientRequest = _messageHubClientsConnection.MessageConverterList.ConfigureMessageHubClientRequestConverter.GetExternalMessage(queueItem.ConnectionMessage);
                        _queueItemTasks.Add(new QueueItemTask(HandleConfigureMessageHubClientRequestAsync(configureMessageHubClientRequest, queueItem.MessageReceivedInfo), queueItem));
                        break;

                    case MessageTypeIds.ExecuteMessageQueueActionRequest:
                        var executeMessageQueueActionRequest = _messageHubClientsConnection.MessageConverterList.ExecuteMessageQueueActionRequestConverter.GetExternalMessage(queueItem.ConnectionMessage);
                        _queueItemTasks.Add(new QueueItemTask(HandleExecuteMessageQueueAction(executeMessageQueueActionRequest, queueItem.MessageReceivedInfo), queueItem));
                        break;

                    case MessageTypeIds.GetMessageHubClientsRequest:
                        var getMessageHubClientsRequest = _messageHubClientsConnection.MessageConverterList.GetMessageHubClientsRequestConverter.GetExternalMessage(queueItem.ConnectionMessage);
                        _queueItemTasks.Add(new QueueItemTask(HandleGetMessageHubClientsRequestAsync(getMessageHubClientsRequest, queueItem.MessageReceivedInfo), queueItem));
                        break;

                    case MessageTypeIds.GetMessageHubsRequest:
                        var getMessageHubsRequest = _messageHubClientsConnection.MessageConverterList.GetMessageHubsRequestConverter.GetExternalMessage(queueItem.ConnectionMessage);
                        _queueItemTasks.Add(new QueueItemTask(HandleGetMessageHubsRequestAsync(getMessageHubsRequest, queueItem.MessageReceivedInfo), queueItem));
                        break;

                    case MessageTypeIds.GetMessageQueuesRequest:
                        var getMessageQueuesRequest = _messageHubClientsConnection.MessageConverterList.GetMessageQueuesRequestConverter.GetExternalMessage(queueItem.ConnectionMessage);
                        _queueItemTasks.Add(new QueueItemTask(HandleGetMessageQueuesRequestAsync(getMessageQueuesRequest, queueItem.MessageReceivedInfo), queueItem));
                        break;
                }
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
            return Task.Factory.StartNew(async () =>
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

        /// <summary>
        /// Handles request to get message hub clients
        /// </summary>
        /// <param name="getMessageHubClientsRequest"></param>
        /// <param name="messageReceivedInfo"></param>
        /// <returns></returns>
        private Task HandleGetMessageHubClientsRequestAsync(GetMessageHubClientsRequest getMessageHubClientsRequest, MessageReceivedInfo messageReceivedInfo)
        {
            return Task.Factory.StartNew(async () =>
            {
                _log.Log(DateTimeOffset.UtcNow, "Information", $"Processing {getMessageHubClientsRequest.TypeId}");

                using (var scope = _serviceProvider.CreateScope())
                {
                    var messageHubClientService = scope.ServiceProvider.GetService<IMessageHubClientService>();
                    var queueMessageHubService = scope.ServiceProvider.GetRequiredService<IQueueMessageHubService>();

                    var response = new GetMessageHubClientsResponse()
                    {
                        Response = new MessageResponse()
                        {
                            IsMore = false,
                            MessageId = getMessageHubClientsRequest.Id,
                            Sequence = 1
                        },
                        MessageHubClients = new()
                    };

                    var messageHubClient = GetMessageHubClientBySecurityKey(getMessageHubClientsRequest.SecurityKey, messageHubClientService);

                    var securityItem = messageHubClient == null ? null : _queueMessageHub.SecurityItems.FirstOrDefault(si => si.MessageHubClientId == messageHubClient.Id);

                    if (messageHubClient == null)
                    {
                        response.Response.ErrorCode = ResponseErrorCodes.PermissionDenied;
                        response.Response.ErrorMessage = EnumUtilities.GetEnumDescription(response.Response.ErrorCode);
                    }
                    else if (securityItem == null || !securityItem.RoleTypes.Contains(RoleTypes.HubReadMessageHubClients))
                    {
                        response.Response.ErrorCode = ResponseErrorCodes.PermissionDenied;
                        response.Response.ErrorMessage = EnumUtilities.GetEnumDescription(response.Response.ErrorCode);
                    }
                    else
                    {
                        var messageHubClients = await messageHubClientService.GetAllAsync();
                        response.MessageHubClients = messageHubClients;
                    }

                    // Send response
                    _messageHubClientsConnection.SendGetMessageHubClientsResponse(response, messageReceivedInfo.RemoteEndpointInfo);
                }

                _log.Log(DateTimeOffset.UtcNow, "Information", $"Processed {getMessageHubClientsRequest.TypeId}");
            });
        }

        /// <summary>
        /// Handles request to get message hubs
        /// </summary>
        /// <param name="getMessageHubsRequest"></param>
        /// <param name="messageReceivedInfo"></param>
        /// <returns></returns>
        private Task HandleGetMessageHubsRequestAsync(GetMessageHubsRequest getMessageHubsRequest, MessageReceivedInfo messageReceivedInfo)
        {
            return Task.Factory.StartNew(async () =>
            {
                _log.Log(DateTimeOffset.UtcNow, "Information", $"Processing {getMessageHubsRequest.TypeId}");

                using (var scope = _serviceProvider.CreateScope())
                {
                    var messageHubClientService= scope.ServiceProvider.GetService<IMessageHubClientService>();
                    var queueMessageHubService = scope.ServiceProvider.GetRequiredService<IQueueMessageHubService>();            

                    var response = new GetMessageHubsResponse()
                    {
                        Response = new MessageResponse()
                        {
                            IsMore = false,
                            MessageId = getMessageHubsRequest.Id,
                            Sequence = 1
                        },
                        MessageHubs = new()
                    };

                    var messageHubClient = GetMessageHubClientBySecurityKey(getMessageHubsRequest.SecurityKey, messageHubClientService);

                    var securityItem = messageHubClient == null ? null : _queueMessageHub.SecurityItems.FirstOrDefault(si => si.MessageHubClientId == messageHubClient.Id);

                    if (messageHubClient == null)
                    {
                        response.Response.ErrorCode = ResponseErrorCodes.PermissionDenied;
                        response.Response.ErrorMessage = EnumUtilities.GetEnumDescription(response.Response.ErrorCode);
                    }
                    else if (securityItem == null || !securityItem.RoleTypes.Contains(RoleTypes.HubReadMessageHubs))
                    {
                        response.Response.ErrorCode = ResponseErrorCodes.PermissionDenied;
                        response.Response.ErrorMessage = EnumUtilities.GetEnumDescription(response.Response.ErrorCode);
                    }
                    else
                    {
                        var queueMessageHubs = await queueMessageHubService.GetAllAsync();
                        response.MessageHubs = queueMessageHubs;
                    }

                    // Send response
                    _messageHubClientsConnection.SendGetMessageHubsResponse(response, messageReceivedInfo.RemoteEndpointInfo);                                        
                }

                _log.Log(DateTimeOffset.UtcNow, "Information", $"Processed {getMessageHubsRequest.TypeId}");
            });
        }

        /// <summary>
        /// Handles request to get message queues
        /// </summary>
        /// <param name="getMessageQueuesRequest"></param>
        /// <param name="messageReceivedInfo"></param>
        /// <returns></returns>
        private Task HandleGetMessageQueuesRequestAsync(GetMessageQueuesRequest getMessageQueuesRequest, MessageReceivedInfo messageReceivedInfo)
        {
            return Task.Factory.StartNew(async () =>
            {
                _log.Log(DateTimeOffset.UtcNow, "Information", $"Processing {getMessageQueuesRequest.TypeId} (Security Key={getMessageQueuesRequest.SecurityKey})");

                using (var scope = _serviceProvider.CreateScope())
                {
                    var messageHubClientService = scope.ServiceProvider.GetRequiredService<IMessageHubClientService>();
                    var messageQueueService = scope.ServiceProvider.GetRequiredService<IMessageQueueService>();
                    
                    var response = new GetMessageQueuesResponse()
                    {
                        Response = new MessageResponse()
                        {
                            IsMore = false,
                            MessageId = getMessageQueuesRequest.Id,
                            Sequence = 1
                        },
                        MessageQueues = new()
                    };

                    var messageHubClient = GetMessageHubClientBySecurityKey(getMessageQueuesRequest.SecurityKey, messageHubClientService);

                    var securityItem = messageHubClient == null ? null : _queueMessageHub.SecurityItems.FirstOrDefault(si => si.MessageHubClientId == messageHubClient.Id);

                    if (messageHubClient == null)
                    {                        
                        response.Response.ErrorCode = ResponseErrorCodes.PermissionDenied;
                        response.Response.ErrorMessage = EnumUtilities.GetEnumDescription(response.Response.ErrorCode);
                    }
                    else if (securityItem == null || !securityItem.RoleTypes.Contains(RoleTypes.HubReadMessageQueues))
                    {                        
                        response.Response.ErrorCode = ResponseErrorCodes.PermissionDenied;
                        response.Response.ErrorMessage = EnumUtilities.GetEnumDescription(response.Response.ErrorCode);
                    }
                    else
                    {                        
                        // TODO: Consider filtering which queues are visible. Could check messageHubClient.SecurityItems
                        var messageQueues = await messageQueueService.GetAllAsync();
                        response.MessageQueues = messageQueues;
                    }
                    
                    // Send response
                    _messageHubClientsConnection.SendGetMessageQueuesResponse(response, messageReceivedInfo.RemoteEndpointInfo);                    
                }

                _log.Log(DateTimeOffset.UtcNow, "Information", $"Processed {getMessageQueuesRequest.TypeId}");
            });
        }

        /// <summary>
        /// Handles request to get message hubs
        /// </summary>
        /// <param name="getMessageHubsRequest"></param>
        /// <param name="messageReceivedInfo"></param>
        /// <returns></returns>
        private Task HandleAddMessageHubClientRequestAsync(AddMessageHubClientRequest addMessageHubClientRequest, MessageReceivedInfo messageReceivedInfo)
        {
            return Task.Factory.StartNew(async () =>
            {
                _log.Log(DateTimeOffset.UtcNow, "Information", $"Processing {addMessageHubClientRequest.TypeId}");

                using (var scope = _serviceProvider.CreateScope())
                {
                    var messageHubService = scope.ServiceProvider.GetRequiredService<IQueueMessageHubService>();
                    var messageHubClientService = scope.ServiceProvider.GetRequiredService<IMessageHubClientService>();
                    var queueMessageHubService = scope.ServiceProvider.GetRequiredService<IQueueMessageHubService>();

                    var response = new AddMessageHubClientResponse()
                    {
                        Response = new MessageResponse()
                        {
                            IsMore = false,
                            MessageId = addMessageHubClientRequest.Id,
                            Sequence = 1
                        },
                        MessageHubClientId = ""
                    };

                    var messageHubClient = GetMessageHubClientBySecurityKey(addMessageHubClientRequest.SecurityKey, messageHubClientService);

                    var securityItem = messageHubClient == null ? null :_queueMessageHub.SecurityItems.FirstOrDefault(si => si.MessageHubClientId == messageHubClient.Id);

                    if (messageHubClient == null)
                    {
                        response.Response.ErrorCode = ResponseErrorCodes.PermissionDenied;
                        response.Response.ErrorMessage = EnumUtilities.GetEnumDescription(response.Response.ErrorCode);
                    }
                    else if (securityItem == null || !securityItem.RoleTypes.Contains(RoleTypes.HubAdmin))
                    {
                        response.Response.ErrorCode = ResponseErrorCodes.PermissionDenied;
                        response.Response.ErrorMessage = EnumUtilities.GetEnumDescription(response.Response.ErrorCode);
                    }
                    else if (!IsValidFormatSecurityKey(addMessageHubClientRequest.ClientSecurityKey))
                    {
                        response.Response.ErrorCode = ResponseErrorCodes.InvalidParameters;
                        response.Response.ErrorMessage = $"{EnumUtilities.GetEnumDescription(response.Response.ErrorCode)}: Security Key is an invalid format";
                    }
                    else
                    {
                        // Check if security key is used already
                        var isMessageHubClientForKey = (await messageHubClientService.GetAllAsync())
                                                .Any(c => c.SecurityKey.ToLower() == addMessageHubClientRequest.ClientSecurityKey.ToLower());

                        if (isMessageHubClientForKey)
                        {
                            response.Response.ErrorCode = ResponseErrorCodes.InvalidParameters;
                            response.Response.ErrorMessage = $"{EnumUtilities.GetEnumDescription(response.Response.ErrorCode)}: Security Key must be unique";
                        }
                        else
                        {
                            // Add message hub client
                            var newMessageHubClient = new MessageHubClient()
                            {
                                Id = Guid.NewGuid().ToString(),
                                SecurityKey = addMessageHubClientRequest.ClientSecurityKey
                            };
                            await messageHubClientService.AddAsync(newMessageHubClient);

                            // Update memory cache of message hub clients
                            _messageHubClientsBySecurityKey.Add(newMessageHubClient.SecurityKey, messageHubClient);

                            // Add default permissions so that client can see message hubs & message queues. They will just be able to
                            // see the hubs & queues but not modify them
                            var queueMessageHub = await queueMessageHubService.GetByIdAsync(_queueMessageHub.Id);
                            queueMessageHub.SecurityItems.Add(new SecurityItem()
                            {
                                MessageHubClientId = newMessageHubClient.Id,
                                RoleTypes = RoleTypeUtilities.DefaultNonAdminHubClientRoleTypes                                
                            });
                            await queueMessageHubService.UpdateAsync(queueMessageHub);

                            _queueMessageHub = queueMessageHub;

                            response.MessageHubClientId = newMessageHubClient.Id;
                        }
                    }

                    // Send response
                    _messageHubClientsConnection.SendAddMessageHubClientResponse(response, messageReceivedInfo.RemoteEndpointInfo);
                }

                _log.Log(DateTimeOffset.UtcNow, "Information", $"Processed {addMessageHubClientRequest.TypeId} for {messageReceivedInfo.RemoteEndpointInfo.Ip}:{messageReceivedInfo.RemoteEndpointInfo.Port}");
            });
        }

        /// <summary>
        /// Handles request to execute action against message queue. E.g. Delete, clear etc.
        /// </summary>
        /// <param name="getMessageHubsRequest"></param>
        /// <param name="messageReceivedInfo"></param>
        /// <returns></returns>
        private Task HandleExecuteMessageQueueAction(ExecuteMessageQueueActionRequest executeMessageQueueActionRequest, MessageReceivedInfo messageReceivedInfo)
        {
            return Task.Factory.StartNew(async () =>
            {
                var isHasQueueMutex = false;
                MessageQueueWorker? messageQueueWorker = null;

                try
                {
                    _log.Log(DateTimeOffset.UtcNow, "Information", $"Processing {executeMessageQueueActionRequest.TypeId}");

                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var messageHubService = scope.ServiceProvider.GetRequiredService<IQueueMessageHubService>();
                        var messageHubClientService = scope.ServiceProvider.GetRequiredService<IMessageHubClientService>();
                        var messageQueueService = scope.ServiceProvider.GetRequiredService<IMessageQueueService>();
                        var queueMessageService = scope.ServiceProvider.GetRequiredService<IQueueMessageInternalService>();
                        var queueMessageHubService = scope.ServiceProvider.GetRequiredService<IQueueMessageHubService>();

                        var response = new ExecuteMessageQueueActionResponse()
                        {
                            Response = new MessageResponse()
                            {
                                IsMore = false,
                                MessageId = executeMessageQueueActionRequest.Id,
                                Sequence = 1
                            },
                        };

                        var messageHubClient = GetMessageHubClientBySecurityKey(executeMessageQueueActionRequest.SecurityKey, messageHubClientService);

                        var securityItem = messageHubClient == null ? null : _queueMessageHub.SecurityItems.FirstOrDefault(si => si.MessageHubClientId == messageHubClient.Id);

                        var messageQueue = await messageQueueService.GetByIdAsync(executeMessageQueueActionRequest.MessageQueueId);                        

                        if (messageHubClient == null)
                        {
                            response.Response.ErrorCode = ResponseErrorCodes.PermissionDenied;
                            response.Response.ErrorMessage = EnumUtilities.GetEnumDescription(response.Response.ErrorCode);
                        }
                        else if (securityItem == null || !securityItem.RoleTypes.Contains(RoleTypes.HubAdmin))
                        {
                            response.Response.ErrorCode = ResponseErrorCodes.PermissionDenied;
                            response.Response.ErrorMessage = EnumUtilities.GetEnumDescription(response.Response.ErrorCode);
                        }
                        else if (messageQueue == null)
                        {
                            response.Response.ErrorCode = ResponseErrorCodes.InvalidParameters;
                            response.Response.ErrorMessage = $"{EnumUtilities.GetEnumDescription(response.Response.ErrorCode)}: Message Queue does not exist";
                        }
                        else
                        {
                            // Get message queue worker
                            messageQueueWorker = _messageQueueWorkers.FirstOrDefault(w => w.MessageQueueId == messageQueue.Id);

                            // Lock queue for duration of action
                            isHasQueueMutex = messageQueueWorker== null ? false : messageQueueWorker.QueueMutex.WaitOne();

                            switch (executeMessageQueueActionRequest.ActionName)
                            {
                                case "CLEAR":
                                    _log.Log(DateTimeOffset.UtcNow, "Information", $"Clearing queue {messageQueue.Name}");

                                    // Delete messages
                                    var queueMessages1 = await queueMessageService.GetByMessageQueueAsync(messageQueue.Id);
                                    while (queueMessages1.Any())
                                    {
                                        await queueMessageService.DeleteByIdAsync(queueMessages1.First().Id);
                                        queueMessages1.RemoveAt(0);
                                    }

                                    // Notify queue cleared
                                    if (messageQueueWorker != null)
                                    {
                                        messageQueueWorker.NotifyQueueCleared();
                                    }

                                    _log.Log(DateTimeOffset.UtcNow, "Information", $"Cleared queue {messageQueue.Name}");
                                    break;

                                case "DELETE":
                                    _log.Log(DateTimeOffset.UtcNow, "Information", $"Deleting queue {messageQueue.Name}");

                                    // Stop message queue worker                            
                                    if (messageQueueWorker != null)
                                    {
                                        _log.Log(DateTimeOffset.UtcNow, "Information", $"Stopping queue worker for {messageQueue.Name} because queue is being deleted");

                                        messageQueueWorker.Stop();
                                        _messageQueueWorkers.Remove(messageQueueWorker);
                                    }

                                    // Delete messages
                                    var queueMessages2 = await queueMessageService.GetByMessageQueueAsync(messageQueue.Id);
                                    while (queueMessages2.Any())
                                    {
                                        await queueMessageService.DeleteByIdAsync(queueMessages2.First().Id);
                                        queueMessages2.RemoveAt(0);
                                    }

                                    // Delete message queue
                                    await messageQueueService.DeleteByIdAsync(messageQueue.Id);

                                    _log.Log(DateTimeOffset.UtcNow, "Information", $"Deleted queue {messageQueue.Name}");
                                    break;

                                default:
                                    response.Response.ErrorCode = ResponseErrorCodes.InvalidParameters;
                                    response.Response.ErrorMessage = $"{EnumUtilities.GetEnumDescription(response.Response.ErrorCode)}: Action Name {executeMessageQueueActionRequest.ActionName} is invalid";
                                    break;
                            }
                        }

                        // Send response
                        _messageHubClientsConnection.SendExecuteMessageQueueActionResponse(response, messageReceivedInfo.RemoteEndpointInfo);
                    }
                }
                finally
                {
                    if (isHasQueueMutex && messageQueueWorker != null) messageQueueWorker.QueueMutex.ReleaseMutex();
                }

                _log.Log(DateTimeOffset.UtcNow, "Information", $"Processed {executeMessageQueueActionRequest.TypeId}");
            });
        }

        /// <summary>
        /// Handles request to configure message hub client
        /// </summary>
        /// <param name="getMessageHubsRequest"></param>
        /// <param name="messageReceivedInfo"></param>
        /// <returns></returns>
        private Task HandleConfigureMessageHubClientRequestAsync(ConfigureMessageHubClientRequest configureMessageHubClientRequest, MessageReceivedInfo messageReceivedInfo)
        {
            return Task.Factory.StartNew(async () =>
            {              
                _log.Log(DateTimeOffset.UtcNow, "Information", $"Processing {configureMessageHubClientRequest.TypeId}");

                using (var scope = _serviceProvider.CreateScope())
                {
                    var messageHubService = scope.ServiceProvider.GetRequiredService<IQueueMessageHubService>();
                    var messageHubClientService = scope.ServiceProvider.GetRequiredService<IMessageHubClientService>();
                    var messageQueueService = scope.ServiceProvider.GetRequiredService<IMessageQueueService>();
                    var queueMessageHubService = scope.ServiceProvider.GetRequiredService<IQueueMessageHubService>();

                    var response = new ConfigureMessageHubClientResponse()
                    {
                        Response = new MessageResponse()
                        {
                            IsMore = false,
                            MessageId = configureMessageHubClientRequest.Id,
                            Sequence = 1
                        },                        
                    };

                    var messageHubClient = GetMessageHubClientBySecurityKey(configureMessageHubClientRequest.SecurityKey, messageHubClientService);

                    var messageHubClientEdit = await messageHubClientService.GetByIdAsync(configureMessageHubClientRequest.MessageHubClientId);

                    var securityItem = messageHubClient == null ? null : _queueMessageHub.SecurityItems.FirstOrDefault(si => si.MessageHubClientId == messageHubClient.Id);

                    if (messageHubClient == null)
                    {
                        response.Response.ErrorCode = ResponseErrorCodes.PermissionDenied;
                        response.Response.ErrorMessage = EnumUtilities.GetEnumDescription(response.Response.ErrorCode);
                    }
                    else if (securityItem == null || !securityItem.RoleTypes.Contains(RoleTypes.HubAdmin))
                    {
                        response.Response.ErrorCode = ResponseErrorCodes.PermissionDenied;
                        response.Response.ErrorMessage = EnumUtilities.GetEnumDescription(response.Response.ErrorCode);
                    }
                    else if (messageHubClientEdit == null)
                    {
                        response.Response.ErrorCode = ResponseErrorCodes.InvalidParameters;
                        response.Response.ErrorMessage = $"{EnumUtilities.GetEnumDescription(response.Response.ErrorCode)}: Message Hub Client Id is invalid";
                    }
                    else if (String.IsNullOrEmpty(configureMessageHubClientRequest.MessageQueueId) &&
                            configureMessageHubClientRequest.RoleTypes.Except(RoleTypeUtilities.HubRoleTypes).Any())
                    {
                        response.Response.ErrorCode = ResponseErrorCodes.InvalidParameters;
                        response.Response.ErrorMessage = $"{EnumUtilities.GetEnumDescription(response.Response.ErrorCode)}: Some role types are not valid for the hub";
                    }
                    else if (!String.IsNullOrEmpty(configureMessageHubClientRequest.MessageQueueId) &&
                            configureMessageHubClientRequest.RoleTypes.Except(RoleTypeUtilities.QueueRoleTypes).Any())
                    {
                        response.Response.ErrorCode = ResponseErrorCodes.InvalidParameters;
                        response.Response.ErrorMessage = $"{EnumUtilities.GetEnumDescription(response.Response.ErrorCode)}: Some role types are not valid for queues";
                    }
                    else
                    {
                        if (String.IsNullOrEmpty(configureMessageHubClientRequest.MessageQueueId))   // Hub config
                        {               
                                var queueMessageHub = await queueMessageHubService.GetByIdAsync(_queueMessageHub.Id);

                                // Update SecurityItem.RoleTypes
                                var securityItemEdit = queueMessageHub.SecurityItems.FirstOrDefault(si => si.MessageHubClientId == messageHubClientEdit.Id);
                                if (securityItemEdit == null)
                                {
                                    securityItemEdit = new SecurityItem()
                                    {
                                        MessageHubClientId = messageHubClientEdit.Id,
                                        RoleTypes = configureMessageHubClientRequest.RoleTypes
                                    };
                                    queueMessageHub.SecurityItems.Add(securityItemEdit);
                                }
                                else
                                {
                                    securityItemEdit.RoleTypes = configureMessageHubClientRequest.RoleTypes;
                                }

                                // Remove security item if no roles
                                if (!securityItemEdit.RoleTypes.Any())
                                {
                                    queueMessageHub.SecurityItems.Remove(securityItemEdit);
                                }

                                await queueMessageHubService.UpdateAsync(queueMessageHub);

                                _queueMessageHub = queueMessageHub;                            
                        }
                        else     // Message queue config
                        {

                            var messageQueue = await messageQueueService.GetByIdAsync(configureMessageHubClientRequest.MessageQueueId);
                            
                            if (messageQueue == null)
                            {
                                response.Response.ErrorCode = ResponseErrorCodes.InvalidParameters;
                                response.Response.ErrorMessage = $"{EnumUtilities.GetEnumDescription(response.Response.ErrorCode)}: Message Queue Id";
                            }
                            else
                            {
                                var securityItemEdit = messageQueue.SecurityItems.FirstOrDefault(si => si.MessageHubClientId == messageHubClientEdit.Id);
                                if (securityItemEdit == null)
                                {
                                    securityItemEdit = new SecurityItem()
                                    {
                                        MessageHubClientId = messageHubClientEdit.Id,
                                        RoleTypes = configureMessageHubClientRequest.RoleTypes
                                    };
                                    messageQueue.SecurityItems.Add(securityItemEdit);
                                }
                                else
                                {
                                    securityItemEdit.RoleTypes = configureMessageHubClientRequest.RoleTypes;
                                }

                                // Remove security item if not roles
                                if (!securityItemEdit.RoleTypes.Any())
                                {
                                    messageQueue.SecurityItems.Remove(securityItemEdit);
                                }

                                await messageQueueService.UpdateAsync(messageQueue);
                            }
                        }
                    }

                    // Send response
                    _messageHubClientsConnection.SendConfigureMessageHubClientResponse(response, messageReceivedInfo.RemoteEndpointInfo);
                }

                _log.Log(DateTimeOffset.UtcNow, "Information", $"Processed {configureMessageHubClientRequest.TypeId}");
            });
        }

        /// <summary>
        /// Handles add message queue
        /// </summary>
        /// <param name="getMessageHubsRequest"></param>
        /// <param name="messageReceivedInfo"></param>
        /// <returns></returns>
        private Task HandleAddMessageQueueRequestAsync(AddMessageQueueRequest addMessageQueueRequest, MessageReceivedInfo messageReceivedInfo)
        {
            return Task.Factory.StartNew(async () =>
            {
                _log.Log(DateTimeOffset.UtcNow, "Information", $"Processing {addMessageQueueRequest.TypeId}");

                using (var scope = _serviceProvider.CreateScope())
                {
                    var messageHubService = scope.ServiceProvider.GetRequiredService<IQueueMessageHubService>();
                    var messageHubClientService = scope.ServiceProvider.GetRequiredService<IMessageHubClientService>();
                    var messageQueueService = scope.ServiceProvider.GetRequiredService<IMessageQueueService>();
                    var queueMessageHubService = scope.ServiceProvider.GetRequiredService<IQueueMessageHubService>();

                    var response = new AddMessageQueueResponse()
                    {
                        Response = new MessageResponse()
                        {
                            IsMore = false,
                            MessageId = addMessageQueueRequest.Id,
                            Sequence = 1
                        },                        
                    };

                    var messageHubClient = GetMessageHubClientBySecurityKey(addMessageQueueRequest.SecurityKey, messageHubClientService);                    

                    var securityItem = messageHubClient == null ? null : _queueMessageHub.SecurityItems.FirstOrDefault(si => si.MessageHubClientId == messageHubClient.Id);

                    if (messageHubClient == null)
                    {
                        response.Response.ErrorCode = ResponseErrorCodes.PermissionDenied;
                        response.Response.ErrorMessage = EnumUtilities.GetEnumDescription(response.Response.ErrorCode);
                    }
                    else if (securityItem == null || !securityItem.RoleTypes.Contains(RoleTypes.HubAdmin))
                    {
                        response.Response.ErrorCode = ResponseErrorCodes.PermissionDenied;
                        response.Response.ErrorMessage = EnumUtilities.GetEnumDescription(response.Response.ErrorCode);
                    }
                    else
                    {
                        var messageQueues = await messageQueueService.GetAllAsync();

                        if (messageQueues.Any(q => q.Name.ToLower() == addMessageQueueRequest.MessageQueueName.ToLower()))
                        {
                            response.Response.ErrorCode = ResponseErrorCodes.InvalidParameters;
                            response.Response.ErrorMessage = $"{EnumUtilities.GetEnumDescription(response.Response.ErrorCode)}: Queue Name must be unique";
                        }
                        else
                        {
                            // Add message queue
                            var messageQueue = new MessageQueue()
                            {
                                Id = Guid.NewGuid().ToString(),
                                Name = addMessageQueueRequest.MessageQueueName,
                                Ip = NetworkUtilities.GetLocalIPV4Addresses()[0],
                                Port = GetFreeQueuePort(messageQueues),
                                MaxConcurrentProcessing = addMessageQueueRequest.MaxConcurrentProcessing,
                                MaxSize = addMessageQueueRequest.MaxSize,
                                SecurityItems = new List<SecurityItem>()
                            };

                            if (messageQueue.Port == 0)    // No free port
                            {
                                response.Response.ErrorCode = ResponseErrorCodes.Unknown;
                                response.Response.ErrorMessage = "No free ports";
                            }
                            else
                            {
                                // Give every hub admin default permissions for queue. Saves them having to explicitly set permissions afterwards.
                                var messageHub = await messageHubService.GetByIdAsync(_queueMessageHub.Id);
                                foreach(var currentSecurityItem in messageHub.SecurityItems.Where(si => si.RoleTypes.Contains(RoleTypes.HubAdmin)))
                                {
                                    messageQueue.SecurityItems.Add(new SecurityItem()
                                    {
                                        MessageHubClientId = currentSecurityItem.MessageHubClientId,
                                        RoleTypes = RoleTypeUtilities.DefaultAdminQueueClientRoleTypes
                                    });
                                }

                                await messageQueueService.AddAsync(messageQueue);

                                response.MessageQueueId = messageQueue.Id;

                                // Add message queue worker
                                var messageQueueWorker = new MessageQueueWorker(messageQueue, _serviceProvider);
                                messageQueueWorker.Start();
                                _messageQueueWorkers.Add(messageQueueWorker);
                            }
                        }
                    }

                    // Send response
                    _messageHubClientsConnection.SendAddMessageQueueResponse(response, messageReceivedInfo.RemoteEndpointInfo);                    
                }

                _log.Log(DateTimeOffset.UtcNow, "Information", $"Processed {addMessageQueueRequest.TypeId}");
            });
        }

        /// <summary>
        /// Returns a free port or 0 if none
        /// </summary>
        /// <param name="messageQueues"></param>
        /// <returns></returns>
        private int GetFreeQueuePort(List<MessageQueue> messageQueues)
        {
            int port = _systemConfig.MinQueuePort - 1;

            do
            {
                port++;

                if (!messageQueues.Any(q => q.Port == port))
                {
                    if (NetworkUtilities.IsLocalPortFree(port)) return port;
                }
                else if (port >= _systemConfig.MaxQueuePort)
                {
                    return 0;
                }
            } while (true);           
        }

        private MessageHubClient? GetMessageHubClientBySecurityKey(string securityKey, IMessageHubClientService messageHubClientService)
        {            
            if (_messageHubClientsLastRefresh.AddMinutes(5) <= DateTimeOffset.UtcNow)       // Periodic refresh
            {                
                _messageHubClientsBySecurityKey.Clear();
            }
            if (!_messageHubClientsBySecurityKey.Any())   // Cache empty, load it
            {
                RefreshMessageHubClients(messageHubClientService);
            }
            return _messageHubClientsBySecurityKey.ContainsKey(securityKey) ? _messageHubClientsBySecurityKey[securityKey] : null;
        }

        private void RefreshMessageHubClients(IMessageHubClientService messageHubClientService)
        {
            _messageHubClientsLastRefresh = DateTimeOffset.UtcNow;

            _messageHubClientsBySecurityKey.Clear();
            var messageHubClients = messageHubClientService.GetAll();
            foreach (var messageHubClient in messageHubClients)
            {
                _messageHubClientsBySecurityKey.TryAdd(messageHubClient.SecurityKey, messageHubClient);
            }
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
                    case QueueItemTypes.ConnectionMessage:
                        _log.Log(DateTimeOffset.UtcNow, "Information", $"Processed task {queueItemTask.QueueItem.ConnectionMessage.TypeId}");
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
            return Task.Factory.StartNew(() =>
            {
                _log.Log(DateTimeOffset.UtcNow, "Information", "Archiving logs");

                DateTimeOffset date = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromDays(_systemConfig.MaxLogDays));

                _lastArchiveLogsTime = DateTimeOffset.UtcNow;

                for (int index = 0; index < 30; index++)
                {
                    // Delete simple log
                    var logFile = Path.Combine(_systemConfig.LogFolder, $"MessageQueueHub-Simple-{date.Subtract(TimeSpan.FromDays(index)).ToString("yyyy-MM-dd")}.txt");
                    if (File.Exists(logFile))
                    {
                        File.Delete(logFile);
                    }

                    // Delete audit log
                    logFile = Path.Combine(_systemConfig.LogFolder, $"MessageQueueHub-Audit-{date.Subtract(TimeSpan.FromDays(index)).ToString("yyyy-MM-dd")}.txt");
                    if (File.Exists(logFile))
                    {
                        File.Delete(logFile);
                    }
                }

                _log.Log(DateTimeOffset.UtcNow, "Information", "Archived logs");
            });
        }

        /// <summary>
        /// Whether security key is valid format
        /// </summary>
        /// <param name="securityKey"></param>
        /// <returns></returns>
        private static bool IsValidFormatSecurityKey(string securityKey)
        {
            return securityKey.Length > 10 && securityKey.Length <= 1024;                    
        }
    }
}
