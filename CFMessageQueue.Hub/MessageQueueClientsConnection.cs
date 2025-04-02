using CFConnectionMessaging;
using CFConnectionMessaging.Models;
using CFMessageQueue.Constants;
using CFMessageQueue.Interfaces;
using CFMessageQueue.Models;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace CFMessageQueue.Hub
{
    /// <summary>
    /// Connection for clients for single message queue. Clients connection via specific port (TCP)
    /// </summary>
    public class MessageQueueClientsConnection
    {
        private readonly ConnectionTcp _connection = new ConnectionTcp();

        private MessageConverterList _messageConverterList = new();

        private readonly IServiceProvider _serviceProvider;

        public delegate void ConnectionMessageReceived(ConnectionMessage connectionMessage, MessageReceivedInfo messageReceivedInfo);
        public event ConnectionMessageReceived? OnConnectionMessageReceived;

        public MessageQueueClientsConnection(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;

            _connection.OnConnectionMessageReceived += delegate (ConnectionMessage connectionMessage, MessageReceivedInfo messageReceivedInfo)
            {
                if (IsResponseMessage(connectionMessage))     // Inside Send... method waiting for response
                {
                    // No current requests do this
                }
                else
                {
                    if (OnConnectionMessageReceived != null)
                    {
                        OnConnectionMessageReceived(connectionMessage, messageReceivedInfo);
                    }
                }
            };
        }

        public void StartListening(int port)
        {
            //_log.Log(DateTimeOffset.UtcNow, "Information", $"Listening on port {port}");

            _connection.ReceivePort = port;
            _connection.StartListening();
        }

        public void StopListening()
        {
            _log.Log(DateTimeOffset.UtcNow, "Information", "Stopping listening");
            _connection.StopListening();
        }


        private void _connectionTcp_OnConnectionMessageReceived(CFConnectionMessaging.Models.ConnectionMessage connectionMessage, CFConnectionMessaging.Models.MessageReceivedInfo messageReceivedInfo)
        {
            throw new NotImplementedException();
        }

        private bool IsResponseMessage(ConnectionMessage connectionMessage)
        {
            return false;
        }

        public Task HandleConnectionMessageAsync(ConnectionMessage connectionMessage, MessageReceivedInfo messageReceivedInfo)
        {
            switch(connectionMessage.TypeId)
            {
                case MessageTypeIds.AddQueueMessageRequest:
                    var addQueueMessageRequest = _messageConverterList.AddQueueMessageRequestConverter.GetExternalMessage(connectionMessage);
                    return HandleAddQueueMessageRequestAsync(addQueueMessageRequest, messageReceivedInfo);

                case MessageTypeIds.GetMessageHubsRequest:
                    var getMessageHubsRequest = _messageConverterList.GetMessageHubsRequestConverter.GetExternalMessage(connectionMessage);
                    return HandleGetMessageHubsRequestAsync(getMessageHubsRequest, messageReceivedInfo);

                case MessageTypeIds.GetNextQueueMessageRequest:
                    var getNextQueueMessageRequest = _messageConverterList.GetNextQueueMessageRequestConverter.GetExternalMessage(connectionMessage);
                    return HandleGetNextQueueMessageRequestAsync(getNextQueueMessageRequest, messageReceivedInfo);

                case MessageTypeIds.MessageQueueSubscribeRequest:
                    var messageQueueSubscribeRequest = _messageConverterList.MessageQueueSubscribeRequestConverter.GetExternalMessage(connectionMessage);
                    return HandleMessageQueueSubscribeRequestAsync(messageQueueSubscribeRequest, messageReceivedInfo);
            }

            return Task.CompletedTask;
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
                using (var scope = _serviceProvider.CreateScope())
                {
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

                    var queueMessageHubs = await queueMessageHubService.GetAllAsync();
                    response.MessageHubs = queueMessageHubs;

                    // Send response
                    _connection.SendMessage(_messageConverterList.GetMessageHubsResponseConverter.GetConnectionMessage(response), messageReceivedInfo.RemoteEndpointInfo);
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
            return Task.Factory.StartNew(async () =>
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var queueMessageService = scope.ServiceProvider.GetRequiredService<IQueueMessageService>();

                    var response = new AddQueueMessageResponse()
                    {
                        Response = new MessageResponse()
                        {
                            IsMore = false,
                            MessageId = addQueueMessageRequest.Id,
                            Sequence = 1
                        },
                    };

                    await queueMessageService.AddAsync(addQueueMessageRequest.QueueMessage);

                    // Send response
                    _connection.SendMessage(_messageConverterList.AddQueueMessageResponseConverter.GetConnectionMessage(response), messageReceivedInfo.RemoteEndpointInfo);
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
                    var queueMessageService = scope.ServiceProvider.GetRequiredService<IQueueMessageService>();

                    var response = new GetNextQueueMessageResponse()
                    {
                        Response = new MessageResponse()
                        {
                            IsMore = false,
                            MessageId = getNextQueueMessageRequest.Id,
                            Sequence = 1
                        },
                    };

                    // Send response
                    _connection.SendMessage(_messageConverterList.GetNextQueueMessageResponseConverter.GetConnectionMessage(response), messageReceivedInfo.RemoteEndpointInfo);
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
                    var queueMessageService = scope.ServiceProvider.GetRequiredService<IQueueMessageService>();

                    // Save subscription
                    var messageQueueSubscription = new MessageQueueSubscription()
                    {
                        Id = Guid.NewGuid().ToString(),
                        MessageQueueId = messageQueueSubscribeRequest.MessageQueue.Id
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

                    // Send response
                    _connection.SendMessage(_messageConverterList.MessageQueueSubscribeResponseConverter.GetConnectionMessage(response), messageReceivedInfo.RemoteEndpointInfo);
                }
            });
        }
    }
}
