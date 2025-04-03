using CFConnectionMessaging.Models;
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

        public MessageQueueWorker(MessageQueue messageQueue, IServiceProvider serviceProvider)
        {
            _messageQueue = messageQueue;
            _serviceProvider = serviceProvider;

            _messageQueueClientsConnection = new MessageQueueClientsConnection(serviceProvider);            
            _messageQueueClientsConnection.OnConnectionMessageReceived += delegate (ConnectionMessage connectionMessage, MessageReceivedInfo messageReceivedInfo)
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

        public string MessageQueueId => _messageQueue.Id;

        public void Start()
        {
            //_log.Log(DateTimeOffset.UtcNow, "Information", "Worker starting");

            _timer.Enabled = true;

            _messageQueueClientsConnection.StartListening(_messageQueue.Port);
        }

        public void Stop()
        {
           // _log.Log(DateTimeOffset.UtcNow, "Information", "Worker stopping");

            _timer.Enabled = false;

            _messageQueueClientsConnection.StopListening();
        }

        private void _timer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                _timer.Enabled = false;

                // Check if need to expire messages
                if (_lastExpireQueueMessages.Add(_expireMessagesFrequency) <= DateTimeOffset.UtcNow)
                {
                    _queueItems.Append(new QueueItem() { ItemType = QueueItemTypes.ExpireQueueMessages });
                }

                ProcessQueueItems(() =>
                {
                    // Periodic action to do while processing queue items
                });

                CheckCompleteQueueItemTasks(_queueItemTasks);
            }
            catch(Exception exception)
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
                        // Set message queue
                        addQueueMessageRequest.QueueMessage.MessageQueueId = _messageQueue.Id;

                        // Set the sender
                        addQueueMessageRequest.QueueMessage.SenderMessageHubClientId = messageHubClient.Id;

                        await queueMessageService.AddAsync(addQueueMessageRequest.QueueMessage);
                    }

                    // Send response
                    _messageQueueClientsConnection.SendAddQueueMessageResponse(response, messageReceivedInfo);                    
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
            return Task.Factory.StartNew(async () =>
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
                        // TODO: Make this more efficient
                        var queueMessage = (await queueMessageService.GetAllAsync())
                                            .Where(m => m.MessageQueueId == messageQueue.Id)
                                            .Where(m => m.ExpirySeconds == 0 || m.CreatedDateTime.AddSeconds(m.ExpirySeconds) < DateTimeOffset.UtcNow)  // not expired
                                            .OrderBy(m => m.CreatedDateTime).FirstOrDefault();

                        response.QueueMessage = queueMessage;
                    }

                    // Send response
                    _messageQueueClientsConnection.SendGetNextQueueMessageResponse(response, messageReceivedInfo);
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
            return Task.Factory.StartNew(async () =>
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    // TODO: Save subscription
                    var messageHubClientService = scope.ServiceProvider.GetRequiredService<IMessageHubClientService>();
                    var messageQueueService = scope.ServiceProvider.GetRequiredService<IMessageQueueService>();
                    var queueMessageService = scope.ServiceProvider.GetRequiredService<IQueueMessageInternalService>();

                    // Save subscription
                    var messageQueueSubscription = new MessageQueueSubscription()
                    {
                        Id = Guid.NewGuid().ToString(),
                        MessageQueueId = messageQueueSubscribeRequest.MessageQueueId
                    };

                    // Create response
                    var response = new MessageQueueSubscribeResponse()
                    {
                        Response = new MessageResponse()
                        {
                            IsMore = false,
                            MessageId = messageQueueSubscribeRequest.Id,
                            Sequence = 1
                        },
                        SubscribeId = messageQueueSubscription.Id
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
                    else
                    {

                    }

                    // Send response
                    _messageQueueClientsConnection.SendMessageQueueSubscribeResponse(response, messageReceivedInfo);
                }
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
                _lastExpireQueueMessages = DateTimeOffset.UtcNow;

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
    }
}
