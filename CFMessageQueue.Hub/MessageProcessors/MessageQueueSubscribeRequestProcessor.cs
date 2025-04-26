using CFConnectionMessaging.Models;
using CFMessageQueue.Common.Interfaces;
using CFMessageQueue.Constants;
using CFMessageQueue.Enums;
using CFMessageQueue.Hub.Models;
using CFMessageQueue.Interfaces;
using CFMessageQueue.Logging;
using CFMessageQueue.Logs;
using CFMessageQueue.Models;
using CFMessageQueue.Utilities;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CFMessageQueue.Hub.MessageProcessors
{
    internal class MessageQueueSubscribeRequestProcessor : MessageProcessorBase, IMessageProcessor
    {
        public MessageQueueSubscribeRequestProcessor(IAuditLog auditLog, ISimpleLog log, IServiceProvider serviceProvider) : base(auditLog, log, serviceProvider)
        {

        }
        public Task ProcessAsync(MessageBase message, MessageReceivedInfo messageReceivedInfo)
        {
            return Task.Run(async () =>
            {
                var messageQueueSubscribeRequest = (MessageQueueSubscribeRequest)message;
                
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
                            var clientQueueSubscription = _hubResources.ClientQueueSubscriptions.FirstOrDefault(s => s.Id == messageQueueSubscribeRequest.ClientSessionId);

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
                                        _hubResources.ClientQueueSubscriptions.Add(clientQueueSubscription);
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
                                        _hubResources.ClientQueueSubscriptions.Remove(clientQueueSubscription);
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
                    _hubResources.ClientsConnection.SendMessage(response, messageReceivedInfo.RemoteEndpointInfo);

                    _log.Log(DateTimeOffset.UtcNow, "Information", $"Processed {messageQueueSubscribeRequest.TypeId} ({(response.Response.ErrorCode == null ? "Success" : response.Response.ErrorMessage)})");
                }
            });
        }

        public bool CanProcess(MessageBase message)
        {
            return message.TypeId == MessageTypeIds.MessageQueueSubscribeRequest;
        }
    }
}
