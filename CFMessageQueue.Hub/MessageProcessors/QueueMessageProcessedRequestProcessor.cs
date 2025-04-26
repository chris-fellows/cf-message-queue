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
    internal class QueueMessageProcessedRequestProcessor : MessageProcessorBase, IMessageProcessor
    {
        public QueueMessageProcessedRequestProcessor(IAuditLog auditLog, ISimpleLog log, IServiceProvider serviceProvider) : base(auditLog, log, serviceProvider)
        {

        }

        public Task ProcessAsync(MessageBase message, MessageReceivedInfo messageReceivedInfo)
        {
            return Task.Run(async () =>
            {
                var queueMessageProcessedRequest = (QueueMessageProcessedRequest)message;
                var isHasMutex = false;

                _log.Log(DateTimeOffset.UtcNow, "Information", $"Processing {queueMessageProcessedRequest.TypeId}");

                var response = new QueueMessageProcessedResponse()
                {
                    Response = new MessageResponse()
                    {
                        IsMore = false,
                        MessageId = queueMessageProcessedRequest.Id,
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

                        var messageHubClient = GetMessageHubClientBySecurityKey(queueMessageProcessedRequest.SecurityKey, messageHubClientService);

                        var messageQueue = await messageQueueService.GetByIdAsync(queueMessageProcessedRequest.MessageQueueId);

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
                            isHasMutex = _hubResources.QueueMutex.WaitOne();

                            // Check that they're processing this message
                            var queueMessageMemory = _hubResources.QueueMessageInternalProcessing.FirstOrDefault(m => m.Id == queueMessageProcessedRequest.QueueMessageId);
                            if (queueMessageMemory == null)     // Not processing message
                            {
                                response.Response.ErrorCode = ResponseErrorCodes.InvalidParameters;
                                response.Response.ErrorMessage = $"{EnumUtilities.GetEnumDescription(response.Response.ErrorCode)}: Queue Message is not being processed";
                            }
                            else if (queueMessageMemory.ProcessingMessageHubClientId != messageHubClient.Id)   // Another client is processing
                            {
                                response.Response.ErrorCode = ResponseErrorCodes.InvalidParameters;
                                response.Response.ErrorMessage = $"{EnumUtilities.GetEnumDescription(response.Response.ErrorCode)}: Queue Message is being processed by another client";
                            }
                            else
                            {
                                if (queueMessageProcessedRequest.Processed)
                                {
                                    // Delete queue message
                                    await queueMessageService.DeleteByIdAsync(queueMessageProcessedRequest.QueueMessageId);

                                    // Log processed
                                    _auditLog.LogQueueMessage(DateTimeOffset.UtcNow, "PROCESSED", _hubResources.MessageQueue.Name, queueMessageMemory);
                                }
                                else
                                {
                                    // Reset queue message status
                                    var queueMessage = await queueMessageService.GetByIdAsync(queueMessageProcessedRequest.QueueMessageId);

                                    queueMessage.Status = QueueMessageStatuses.Default;
                                    queueMessage.ProcessingMessageHubClientId = "";
                                    queueMessage.ProcessingStartDateTime = DateTimeOffset.MinValue;
                                    queueMessage.MaxProcessingSeconds = 0;

                                    await queueMessageService.UpdateAsync(queueMessage);
                                }

                                _hubResources.QueueMessageInternalProcessing.Remove(queueMessageMemory);
                            }
                        }
                    }
                }
                catch (Exception exception)
                {
                    response.Response.ErrorCode = ResponseErrorCodes.Unknown;
                    response.Response.ErrorMessage = $"{EnumUtilities.GetEnumDescription(response.Response.ErrorCode)}: {exception.Message}";

                    _log.Log(DateTimeOffset.UtcNow, "Error", $"Error processing {queueMessageProcessedRequest.TypeId}: {exception.Message}");
                }
                finally
                {
                    if (isHasMutex) _hubResources.QueueMutex.ReleaseMutex();

                    // Send response
                    _hubResources.ClientsConnection.SendMessage(response, messageReceivedInfo.RemoteEndpointInfo);

                    _log.Log(DateTimeOffset.UtcNow, "Information", $"Processed {queueMessageProcessedRequest.TypeId} ({(response.Response.ErrorCode == null ? "Success" : response.Response.ErrorMessage)})");
                }
            });
        }

        public bool CanProcess(MessageBase message)
        {
            return message.TypeId == MessageTypeIds.QueueMessageProcessedRequest;
        }
    }
}
