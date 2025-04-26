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
    internal class GetQueueMessagesRequestProcessor :MessageProcessorBase, IMessageProcessor
    {
        public GetQueueMessagesRequestProcessor(IAuditLog auditLog, ISimpleLog log, IServiceProvider serviceProvider) : base(auditLog, log, serviceProvider)
        {

        }

        public Task ProcessAsync(MessageBase message, MessageReceivedInfo messageReceivedInfo)
        {
            return Task.Run(async () =>
            {
                var getQueueMessagesRequest = (GetQueueMessagesRequest)message;

                _log.Log(DateTimeOffset.UtcNow, "Information", $"Processing {getQueueMessagesRequest.TypeId}");

                var response = new GetQueueMessagesResponse()
                {
                    Response = new MessageResponse()
                    {
                        IsMore = false,
                        MessageId = getQueueMessagesRequest.Id,
                        Sequence = 1
                    },
                };

                var isHasMutex = false;
                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var messageHubClientService = scope.ServiceProvider.GetRequiredService<IMessageHubClientService>();
                        var messageQueueService = scope.ServiceProvider.GetRequiredService<IMessageQueueService>();
                        var queueMessageService = scope.ServiceProvider.GetRequiredService<IQueueMessageInternalService>();

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
                    }
                }
                catch (Exception exception)
                {
                    response.Response.ErrorCode = ResponseErrorCodes.Unknown;
                    response.Response.ErrorMessage = $"{EnumUtilities.GetEnumDescription(response.Response.ErrorCode)}: {exception.Message}";

                    _log.Log(DateTimeOffset.UtcNow, "Error", $"Error processing {getQueueMessagesRequest.TypeId}: {exception.Message}");
                }
                finally
                {
                    if (isHasMutex) _hubResources.QueueMutex.ReleaseMutex();

                    // Send response
                    _hubResources.ClientsConnection.SendMessage(response, messageReceivedInfo.RemoteEndpointInfo);

                    _log.Log(DateTimeOffset.UtcNow, "Information", $"Processed {getQueueMessagesRequest.TypeId} ({(response.Response.ErrorCode == null ? "Success" : response.Response.ErrorMessage)})");
                }
            });
        }

        public bool CanProcess(MessageBase message)
        {
            return message.TypeId == MessageTypeIds.GetQueueMessagesRequest;
        }
    }
}
