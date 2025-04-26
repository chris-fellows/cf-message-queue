using CFConnectionMessaging.Models;
using CFMessageQueue.Common.Interfaces;
using CFMessageQueue.Constants;
using CFMessageQueue.Enums;
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
    internal class ExecuteMessageQueueActionRequestProcessor : MessageProcessorBase, IMessageProcessor
    {
        public ExecuteMessageQueueActionRequestProcessor(IAuditLog auditLog, ISimpleLog log, IServiceProvider serviceProvider) : base(auditLog, log, serviceProvider)
        {

        }

        public Task ProcessAsync(MessageBase message, MessageReceivedInfo messageReceivedInfo)
        {
            return Task.Run(async () =>
            {
                var executeMessageQueueActionRequest = (ExecuteMessageQueueActionRequest)message;

                var isHasQueueMutex = false;
                MessageQueueWorker? messageQueueWorker = null;

                _log.Log(DateTimeOffset.UtcNow, "Information", $"Processing {executeMessageQueueActionRequest.TypeId}");

                var response = new ExecuteMessageQueueActionResponse()
                {
                    Response = new MessageResponse()
                    {
                        IsMore = false,
                        MessageId = executeMessageQueueActionRequest.Id,
                        Sequence = 1
                    },
                };

                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var messageHubService = scope.ServiceProvider.GetRequiredService<IQueueMessageHubService>();
                        var messageHubClientService = scope.ServiceProvider.GetRequiredService<IMessageHubClientService>();
                        var messageQueueService = scope.ServiceProvider.GetRequiredService<IMessageQueueService>();
                        var queueMessageService = scope.ServiceProvider.GetRequiredService<IQueueMessageInternalService>();
                        var queueMessageHubService = scope.ServiceProvider.GetRequiredService<IQueueMessageHubService>();

                        var messageHubClient = GetMessageHubClientBySecurityKey(executeMessageQueueActionRequest.SecurityKey, messageHubClientService);

                        var securityItem = messageHubClient == null ? null : _hubResources.QueueMessageHub.SecurityItems.FirstOrDefault(si => si.MessageHubClientId == messageHubClient.Id);

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
                            messageQueueWorker = _hubResources.MessageQueueWorkers.FirstOrDefault(w => w.MessageQueueId == messageQueue.Id);

                            // Lock queue for duration of action
                            isHasQueueMutex = messageQueueWorker == null ? false : messageQueueWorker.QueueMutex.WaitOne();

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

                                    // Log queue created
                                    _auditLog.LogQueue(DateTimeOffset.UtcNow, "QUEUE_CLEARED", messageQueue.Name);

                                    _log.Log(DateTimeOffset.UtcNow, "Information", $"Cleared queue {messageQueue.Name}");
                                    break;

                                case "DELETE":
                                    _log.Log(DateTimeOffset.UtcNow, "Information", $"Deleting queue {messageQueue.Name}");

                                    // Stop message queue worker                            
                                    if (messageQueueWorker != null)
                                    {
                                        _log.Log(DateTimeOffset.UtcNow, "Information", $"Stopping queue worker for {messageQueue.Name} because queue is being deleted");

                                        messageQueueWorker.Stop();
                                        _hubResources.MessageQueueWorkers.Remove(messageQueueWorker);
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

                                    // Log queue created
                                    _auditLog.LogQueue(DateTimeOffset.UtcNow, "QUEUE_DELETED", messageQueue.Name);

                                    _log.Log(DateTimeOffset.UtcNow, "Information", $"Deleted queue {messageQueue.Name}");
                                    break;

                                default:
                                    response.Response.ErrorCode = ResponseErrorCodes.InvalidParameters;
                                    response.Response.ErrorMessage = $"{EnumUtilities.GetEnumDescription(response.Response.ErrorCode)}: Action Name {executeMessageQueueActionRequest.ActionName} is invalid";
                                    break;
                            }
                        }
                    }
                }
                catch (Exception exception)
                {
                    response.Response.ErrorCode = ResponseErrorCodes.Unknown;
                    response.Response.ErrorMessage = $"{EnumUtilities.GetEnumDescription(response.Response.ErrorCode)}: {exception.Message}";

                    _log.Log(DateTimeOffset.UtcNow, "Error", $"Error processing {executeMessageQueueActionRequest.TypeId}: {exception.Message}");
                }
                finally
                {
                    // Send response
                    _hubResources.ClientsConnection.SendMessage(response, messageReceivedInfo.RemoteEndpointInfo);

                    _log.Log(DateTimeOffset.UtcNow, "Information", $"Processed {executeMessageQueueActionRequest.TypeId}");

                    if (isHasQueueMutex && messageQueueWorker != null) messageQueueWorker.QueueMutex.ReleaseMutex();
                }

                _log.Log(DateTimeOffset.UtcNow, "Information", $"Processed {executeMessageQueueActionRequest.TypeId} ({(response.Response.ErrorCode == null ? "Success" : response.Response.ErrorMessage)})");
            });
        }

        public bool CanProcess(MessageBase message)
        {
            return message.TypeId == MessageTypeIds.ExecuteMessageQueueActionRequest;
        }
    }
}
