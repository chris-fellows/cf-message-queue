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
    internal class AddQueueMessageRequestProcessor : MessageProcessorBase, IMessageProcessor
    {
        public AddQueueMessageRequestProcessor(IAuditLog auditLog, ISimpleLog log, IServiceProvider serviceProvider) : base(auditLog, log, serviceProvider)
        {

        }

        public Task ProcessAsync(MessageBase message, MessageReceivedInfo messageReceivedInfo)
        {
            return Task.Run(async () =>
            {
                var addQueueMessageRequest = (AddQueueMessageRequest)message;

                _log.Log(DateTimeOffset.UtcNow, "Information", $"Processing {addQueueMessageRequest.TypeId}");

                var isHasMutex = false;

                var response = new AddQueueMessageResponse()
                {
                    Response = new MessageResponse()
                    {
                        IsMore = false,
                        MessageId = addQueueMessageRequest.Id,
                        Sequence = 1
                    },
                };

                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var messageHubClientService = scope.ServiceProvider.GetRequiredService<IMessageHubClientService>();
                        var messageQueueService = scope.ServiceProvider.GetRequiredService<IMessageQueueService>();
                        var queueMessageService = scope.ServiceProvider.GetRequiredService<IQueueMessageInternalService>();

                        var messageHubClient = GetMessageHubClientBySecurityKey(addQueueMessageRequest.SecurityKey, messageHubClientService);

                        var messageQueue = await messageQueueService.GetByIdAsync(addQueueMessageRequest.MessageQueueId);

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
                        else if (securityItem == null || !securityItem.RoleTypes.Contains(RoleTypes.QueueWriteQueue))
                        {
                            response.Response.ErrorCode = ResponseErrorCodes.PermissionDenied;
                            response.Response.ErrorMessage = EnumUtilities.GetEnumDescription(response.Response.ErrorCode);
                        }
                        else if (String.IsNullOrWhiteSpace(addQueueMessageRequest.QueueMessage.TypeId))
                        {
                            response.Response.ErrorCode = ResponseErrorCodes.InvalidParameters;
                            response.Response.ErrorMessage = $"{EnumUtilities.GetEnumDescription(response.Response.ErrorCode)}: Type Id must be set";
                        }
                        else if (addQueueMessageRequest.QueueMessage.ExpirySeconds < 0)
                        {
                            response.Response.ErrorCode = ResponseErrorCodes.InvalidParameters;
                            response.Response.ErrorMessage = $"{EnumUtilities.GetEnumDescription(response.Response.ErrorCode)}: Expiry Seconds is invalid";
                        }
                        else
                        {
                            isHasMutex = _hubResources.QueueMutex.WaitOne();

                            // Check if queue is full                            
                            var isQueueFull = false;
                            if (messageQueue.MaxSize > 0)
                            {
                                var messageCount = (await queueMessageService.GetByMessageQueueAsync(messageQueue.Id)).Count;

                                isQueueFull = messageCount >= messageQueue.MaxSize;
                            }

                            if (isQueueFull)
                            {
                                response.Response.ErrorCode = ResponseErrorCodes.MessageQueueFull;
                                response.Response.ErrorMessage = EnumUtilities.GetEnumDescription(response.Response.ErrorCode);
                            }
                            else
                            {
                                // Create queue message
                                var queueMessage = new QueueMessageInternal()
                                {
                                    Content = addQueueMessageRequest.QueueMessage.Content,
                                    ContentType = addQueueMessageRequest.QueueMessage.ContentType,
                                    CreatedDateTime = DateTime.UtcNow,
                                    ExpirySeconds = addQueueMessageRequest.QueueMessage.ExpirySeconds,
                                    ExpiryDateTime = addQueueMessageRequest.QueueMessage.ExpirySeconds == 0 ?
                                                DateTime.MaxValue : DateTime.UtcNow.AddSeconds(addQueueMessageRequest.QueueMessage.ExpirySeconds),
                                    Id = Guid.NewGuid().ToString(),
                                    MessageQueueId = addQueueMessageRequest.MessageQueueId,
                                    Name = addQueueMessageRequest.QueueMessage.Name,
                                    Priority = addQueueMessageRequest.QueueMessage.Priority,
                                    SenderMessageHubClientId = messageHubClient.Id,
                                    Status = QueueMessageStatuses.Default,
                                    TypeId = addQueueMessageRequest.QueueMessage.TypeId
                                };

                                await queueMessageService.AddAsync(queueMessage);

                                // Log added
                                _auditLog.LogQueueMessage(DateTimeOffset.UtcNow, "ADDED", _hubResources.MessageQueue.Name, queueMessage);

                                // Set QueueMessageId for response
                                response.QueueMessageId = queueMessage.Id;

                                // If client subscription indicates waiting for new message then flag notification
                                _hubResources.ClientQueueSubscriptions.Where(s => s.NotifyIfMessageAdded).ToList()
                                        .ForEach(s =>
                                        {
                                            s.DoNotifyMessageAdded = true;
                                            s.NotifyIfMessageAdded = false;
                                        });
                            }
                        }
                    }
                }
                catch (Exception exception)
                {
                    response.Response.ErrorCode = ResponseErrorCodes.Unknown;
                    response.Response.ErrorMessage = $"{EnumUtilities.GetEnumDescription(response.Response.ErrorCode)}: {exception.Message}";

                    _log.Log(DateTimeOffset.UtcNow, "Error", $"Error processing {addQueueMessageRequest.TypeId}: {exception.Message}");
                }
                finally
                {
                    if (isHasMutex) _hubResources.QueueMutex.ReleaseMutex();

                    // Send response
                    _hubResources.ClientsConnection.SendMessage(response, messageReceivedInfo.RemoteEndpointInfo);

                    _log.Log(DateTimeOffset.UtcNow, "Information", $"Processed {addQueueMessageRequest.TypeId} ({(response.Response.ErrorCode == null ? "Success" : response.Response.ErrorMessage)})");
                }
            });
        }

        public bool CanProcess(MessageBase message)
        {
            return message.TypeId == MessageTypeIds.AddQueueMessageRequest;
        }
    }
}
