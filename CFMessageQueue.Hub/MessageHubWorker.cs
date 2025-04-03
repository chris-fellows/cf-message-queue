using CFConnectionMessaging.Models;
using CFMessageQueue.Common;
using CFMessageQueue.Constants;
using CFMessageQueue.Enums;
using CFMessageQueue.Hub.Enums;
using CFMessageQueue.Hub.Models;
using CFMessageQueue.Interfaces;
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

        public MessageHubWorker(QueueMessageHub queueMessageHub, IServiceProvider serviceProvider,
                                 List<MessageQueueWorker> messageQueueWorkers, 
                                 SystemConfig  systemConfig)
        {
            _queueMessageHub = queueMessageHub;
            _messageQueueWorkers = messageQueueWorkers;
            _serviceProvider = serviceProvider;
            _systemConfig = systemConfig;

            _messageHubClientsConnection = new MessageHubClientsConnection(serviceProvider);
            _messageHubClientsConnection.OnConnectionMessageReceived += delegate (ConnectionMessage connectionMessage, MessageReceivedInfo messageReceivedInfo)
            {
                Console.WriteLine($"Received message {connectionMessage.TypeId} from {messageReceivedInfo.RemoteEndpointInfo.Ip}:{messageReceivedInfo.RemoteEndpointInfo.Port}");

                var queueItem = new QueueItem()
                {
                    ItemType = QueueItemTypes.ConnectionMessage,
                    ConnectionMessage = connectionMessage,
                    MessageReceivedInfo = messageReceivedInfo
                };
                _queueItems.Enqueue(queueItem);

                _timer.Interval = 100;
            };

            _timer = new System.Timers.Timer();
            _timer.Elapsed += _timer_Elapsed;
            _timer.Interval = 5000;
            _timer.Enabled = false;
        }

        public void Start()
        {
            //_log.Log(DateTimeOffset.UtcNow, "Information", "Worker starting");

            _timer.Enabled = true;

            _messageHubClientsConnection.StartListening(_queueMessageHub.Port);
        }

        public void Stop()
        {
            // _log.Log(DateTimeOffset.UtcNow, "Information", "Worker stopping");

            _timer.Enabled = false;

            _messageHubClientsConnection.StopListening();
        }

        private void _timer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                _timer.Enabled = false;

                ProcessQueueItems(() =>
                {
                    // Periodic action to do while processing queue items
                });

                CheckCompleteQueueItemTasks(_queueItemTasks);
            }
            catch (Exception exception)
            {
                Console.WriteLine($"Error executing regular functions: {exception.Message}");
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
                Console.WriteLine($"Processing {getMessageHubsRequest.TypeId}");

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
                    else if (securityItem == null || !securityItem.RoleTypes.Contains(RoleTypes.GetMessageHubs))
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
                    _messageHubClientsConnection.SendGetMessageHubsResponse(response, messageReceivedInfo);                                        
                }

                Console.WriteLine($"Processed {getMessageHubsRequest.TypeId}");
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
                Console.WriteLine($"Processing {getMessageQueuesRequest.TypeId} (Security Key={getMessageQueuesRequest.SecurityKey})");

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
                        Console.WriteLine($"HandleGetMessageQueued: No message hub client (Cache count={_messageHubClientsBySecurityKey.Count})");

                        response.Response.ErrorCode = ResponseErrorCodes.PermissionDenied;
                        response.Response.ErrorMessage = EnumUtilities.GetEnumDescription(response.Response.ErrorCode);
                    }
                    else if (securityItem == null || !securityItem.RoleTypes.Contains(RoleTypes.GetMessageQueues))
                    {
                        if (securityItem == null)
                        {
                            Console.WriteLine("HandleGetMessageQueued: No GetMessageQueues permission 100");
                        }
                        else
                        {
                            Console.WriteLine($"HandleGetMessageQueued: No GetMessageQueues permission 200 {securityItem.MessageHubClientId}");
                        }

                        response.Response.ErrorCode = ResponseErrorCodes.PermissionDenied;
                        response.Response.ErrorMessage = EnumUtilities.GetEnumDescription(response.Response.ErrorCode);
                    }
                    else
                    {
                        Console.WriteLine("HandleGetMessageQueued: Got message queues");

                        // TODO: Consider filtering which queues are visible. Could check messageHubClient.SecurityItems
                        var messageQueues = await messageQueueService.GetAllAsync();
                        response.MessageQueues = messageQueues;
                    }
                    
                    // Send response
                    _messageHubClientsConnection.SendGetMessageQueuesResponse(response, messageReceivedInfo);                    
                }

                Console.WriteLine($"Processed {getMessageQueuesRequest.TypeId}");
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
                Console.WriteLine($"Processing {addMessageHubClientRequest.TypeId}");

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
                    else if (securityItem == null || !securityItem.RoleTypes.Contains(RoleTypes.Admin))
                    {
                        response.Response.ErrorCode = ResponseErrorCodes.PermissionDenied;
                        response.Response.ErrorMessage = EnumUtilities.GetEnumDescription(response.Response.ErrorCode);
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
                            RoleTypes = new List<RoleTypes>()
                            {
                                RoleTypes.GetMessageHubs,
                                RoleTypes.GetMessageQueues
                            }
                        });
                        await queueMessageHubService.UpdateAsync(queueMessageHub);

                        _queueMessageHub = queueMessageHub;

                        response.MessageHubClientId = newMessageHubClient.Id;

                        
                    }

                    // Send response
                    _messageHubClientsConnection.SendAddMessageHubClientResponse(response, messageReceivedInfo);
                }

                Console.WriteLine($"Processed {addMessageHubClientRequest.TypeId} for {messageReceivedInfo.RemoteEndpointInfo.Ip}:{messageReceivedInfo.RemoteEndpointInfo.Port}");
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
                Console.WriteLine($"Processing {executeMessageQueueActionRequest.TypeId}");

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
                    else if (securityItem == null || !securityItem.RoleTypes.Contains(RoleTypes.Admin))
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
                        switch (executeMessageQueueActionRequest.ActionName)
                        {
                            case "CLEAR":
                                Console.WriteLine($"Clearing queue {messageQueue.Name}");

                                // Delete messages
                                var queueMessages1 = (await queueMessageService.GetAllAsync())
                                               .Where(m => m.MessageQueueId == messageQueue.Id).ToList();
                                while (queueMessages1.Any())
                                {
                                    await queueMessageService.DeleteByIdAsync(queueMessages1.First().Id);
                                    queueMessages1.RemoveAt(0);
                                }

                                Console.WriteLine($"Cleared queue {messageQueue.Name}");
                                break;

                            case "DELETE":
                                Console.WriteLine($"Deleting queue {messageQueue.Name}");

                                // Stop message queue worker
                                var messageQueueWorker = _messageQueueWorkers.FirstOrDefault(w => w.MessageQueueId == messageQueue.Id);
                                if (messageQueueWorker != null)
                                {
                                    Console.WriteLine($"Stopping queue worker for {messageQueue.Name} because queue is being deleted");

                                    messageQueueWorker.Stop();
                                    _messageQueueWorkers.Remove(messageQueueWorker);
                                }

                                // Delete messages
                                var queueMessages2 = (await queueMessageService.GetAllAsync())
                                            .Where(m => m.MessageQueueId == messageQueue.Id).ToList();
                                while (queueMessages2.Any())
                                {
                                    await queueMessageService.DeleteByIdAsync(queueMessages2.First().Id);
                                    queueMessages2.RemoveAt(0);
                                }

                                // Delete message queue
                                await messageQueueService.DeleteByIdAsync(messageQueue.Id);

                                Console.WriteLine($"Deleted queue {messageQueue.Name}");
                                break;

                            default:
                                response.Response.ErrorCode = ResponseErrorCodes.InvalidParameters;
                                response.Response.ErrorMessage = $"{EnumUtilities.GetEnumDescription(response.Response.ErrorCode)}: Action Name is invalid";
                                break;
                        }
                    }
                       
                    // Send response
                    _messageHubClientsConnection.SendExecuteMessageQueueActionResponse(response, messageReceivedInfo);
                }

                Console.WriteLine($"Processed {executeMessageQueueActionRequest.TypeId}");
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
                Console.WriteLine($"Processing {configureMessageHubClientRequest.TypeId}");

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
                    else if (securityItem == null || !securityItem.RoleTypes.Contains(RoleTypes.Admin))
                    {
                        response.Response.ErrorCode = ResponseErrorCodes.PermissionDenied;
                        response.Response.ErrorMessage = EnumUtilities.GetEnumDescription(response.Response.ErrorCode);
                    }
                    else if (messageHubClientEdit == null)
                    {
                        response.Response.ErrorCode = ResponseErrorCodes.InvalidParameters;
                        response.Response.ErrorMessage = $"{EnumUtilities.GetEnumDescription(response.Response.ErrorCode)}: Message Hub Client Id is invalid";
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

                            // Remove security item if not roles
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
                    _messageHubClientsConnection.SendConfigureMessageHubClientResponse(response, messageReceivedInfo);
                }

                Console.WriteLine($"Processed {configureMessageHubClientRequest.TypeId}");
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
                Console.WriteLine($"Processing {addMessageQueueRequest.TypeId}");

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
                    else if (securityItem == null || !securityItem.RoleTypes.Contains(RoleTypes.Admin))
                    {
                        response.Response.ErrorCode = ResponseErrorCodes.PermissionDenied;
                        response.Response.ErrorMessage = EnumUtilities.GetEnumDescription(response.Response.ErrorCode);
                    }
                    else
                    {
                        var messageQueues = await messageQueueService.GetAllAsync();                        

                        // Add message queue
                        var messageQueue = new MessageQueue()
                        {
                            Id = Guid.NewGuid().ToString(),
                            Name = addMessageQueueRequest.MessageQueueName,
                            Ip = NetworkUtilities.GetLocalIPV4Addresses()[0],
                            Port = GetFreeQueuePort(messageQueues),
                            SecurityItems = new List<SecurityItem>()
                        };

                        if (messageQueue.Port == 0)    // No free port
                        {
                            response.Response.ErrorCode = ResponseErrorCodes.Unknown;
                            response.Response.ErrorMessage = "No free ports";
                        }
                        else
                        {
                            // If message hub has Admin role then give hub client full permissions on queue
                            var queueMessageHub = await messageHubService.GetByIdAsync(_queueMessageHub.Id);
                            if (queueMessageHub.SecurityItems.Any(si => si.RoleTypes.Contains(RoleTypes.Admin)))
                            {
                                queueMessageHub.SecurityItems.Add(new SecurityItem()
                                {
                                    MessageHubClientId = messageHubClient.Id,
                                    RoleTypes = new List<RoleTypes>()
                                {
                                    RoleTypes.ReadQueue,
                                    RoleTypes.WriteQueue,
                                    RoleTypes.SubscribeQueue
                                }
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

                    // Send response
                    _messageHubClientsConnection.SendAddMessageQueueResponse(response, messageReceivedInfo);                    
                }

                Console.WriteLine($"Processed {addMessageQueueRequest.TypeId}");
            });
        }

        /// <summary>
        /// Returns a free port or 0 if none
        /// </summary>
        /// <param name="messageQueues"></param>
        /// <returns></returns>
        private int GetFreeQueuePort(List<MessageQueue> messageQueues)
        {
            int localPort = _systemConfig.MinQueuePort - 1;

            do
            {
                localPort++;

                if (!messageQueues.Any(q => q.Port == localPort))
                {
                    return localPort;
                }
                else if (localPort >= _systemConfig.MaxQueuePort)
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
                        Console.WriteLine($"Processed task {queueItemTask.QueueItem.ConnectionMessage.TypeId}");
                        break;
                    default:
                        Console.WriteLine($"Processed task {queueItemTask.QueueItem.ItemType}");
                        break;
                }
            }
            else
            {
                Console.WriteLine($"Error processing task {queueItemTask.QueueItem.ItemType}: {queueItemTask.Task.Exception.Message}");
            }
        }
    }
}
