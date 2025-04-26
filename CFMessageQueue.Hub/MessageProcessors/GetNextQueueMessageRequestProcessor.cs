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
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CFMessageQueue.Hub.MessageProcessors
{
    internal class GetNextQueueMessageRequestProcessor : MessageProcessorBase, IMessageProcessor
    {
        public GetNextQueueMessageRequestProcessor(IAuditLog auditLog, ISimpleLog log, IServiceProvider serviceProvider) : base(auditLog, log, serviceProvider)
        {

        }

        public Task ProcessAsync(MessageBase message, MessageReceivedInfo messageReceivedInfo)
        {
            return Task.Run(async () =>
            {
                var getNextQueueMessageRequest = (GetNextQueueMessageRequest)message;

                _log.Log(DateTimeOffset.UtcNow, "Information", $"Processing {getNextQueueMessageRequest.TypeId}");

                var isHasMutex = false;

                var response = new GetNextQueueMessageResponse()
                {
                    Response = new MessageResponse()
                    {
                        IsMore = false,
                        MessageId = getNextQueueMessageRequest.Id,
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

                        var messageHubClient = GetMessageHubClientBySecurityKey(getNextQueueMessageRequest.SecurityKey, messageHubClientService);

                        var messageQueue = await messageQueueService.GetByIdAsync(getNextQueueMessageRequest.MessageQueueId);

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
                        else if (securityItem == null || !securityItem.RoleTypes.Contains(RoleTypes.QueueReadQueue))
                        {
                            response.Response.ErrorCode = ResponseErrorCodes.PermissionDenied;
                            response.Response.ErrorMessage = EnumUtilities.GetEnumDescription(response.Response.ErrorCode);
                        }
                        else if (getNextQueueMessageRequest.MaxProcessingSeconds < 1)
                        {
                            response.Response.ErrorCode = ResponseErrorCodes.InvalidParameters;
                            response.Response.ErrorMessage = $"{EnumUtilities.GetEnumDescription(response.Response.ErrorCode)}: Message Processing Seconds must be 1 second or more";
                        }
                        else
                        {
                            // Try and get message, if no message then may need to wait for message
                            bool waitedForMessage = false;
                            QueueMessageInternal? queueMessage = null;
                            var stopwatch = new Stopwatch();    // Started on first failure to get a message from DB
                            while (!waitedForMessage)
                            {
                                // Get mutex. Need to hold it for shortest time possible
                                isHasMutex = _hubResources.QueueMutex.WaitOne();

                                // Get next message. Only allowed if not exceeded max number of concurrent messages being processed                             
                                if (messageQueue.MaxConcurrentProcessing == 0 ||
                                    _hubResources.QueueMessageInternalProcessing.Count < messageQueue.MaxConcurrentProcessing)
                                {
                                    queueMessage = await queueMessageService.GetNextAsync(messageQueue.Id);
                                }

                                if (queueMessage != null)
                                {
                                    // Update message as Processing
                                    queueMessage.Status = QueueMessageStatuses.Processing;
                                    queueMessage.ProcessingMessageHubClientId = messageHubClient.Id;
                                    queueMessage.MaxProcessingSeconds = getNextQueueMessageRequest.MaxProcessingSeconds;
                                    queueMessage.ProcessingStartDateTime = DateTimeOffset.UtcNow;

                                    await queueMessageService.UpdateAsync(queueMessage);

                                    // Set message as processing
                                    _hubResources.QueueMessageInternalProcessing.Add(queueMessage);

                                    waitedForMessage = true;
                                }

                                // Release mutex
                                _hubResources.QueueMutex.ReleaseMutex();
                                isHasMutex = false;

                                // Handle no message
                                if (queueMessage == null)    // No message
                                {
                                    if (getNextQueueMessageRequest.MaxWaitMilliseconds == 0)   // No wait
                                    {
                                        waitedForMessage = true;
                                    }
                                    else      // Wait until message or timeout
                                    {
                                        if (stopwatch.IsRunning)
                                        {
                                            if (stopwatch.ElapsedMilliseconds < getNextQueueMessageRequest.MaxWaitMilliseconds)
                                            {
                                                Thread.Sleep(250);
                                            }
                                            else     // Timeout
                                            {
                                                waitedForMessage = true;
                                            }
                                        }
                                        else   // Start stopwatch for first time
                                        {
                                            stopwatch.Start();
                                        }
                                    }
                                }
                            }

                            response.QueueMessage = queueMessage;

                            // If there's no message then we should notify subscriptions when new message is added
                            if (queueMessage == null)
                            {
                                _hubResources.ClientQueueSubscriptions.ForEach(s =>
                                {
                                    s.NotifyIfMessageAdded = true;
                                    s.DoNotifyMessageAdded = false;
                                });
                            }
                        }
                    }
                }
                catch (Exception exception)
                {
                    response.Response.ErrorCode = ResponseErrorCodes.Unknown;
                    response.Response.ErrorMessage = $"{EnumUtilities.GetEnumDescription(response.Response.ErrorCode)}: {exception.Message}";

                    _log.Log(DateTimeOffset.UtcNow, "Error", $"Error processing {getNextQueueMessageRequest.TypeId}: {exception.Message}");
                }
                finally
                {
                    if (isHasMutex) _hubResources.QueueMutex.ReleaseMutex();

                    // Send response
                    _hubResources.ClientsConnection.SendMessage(response, messageReceivedInfo.RemoteEndpointInfo);

                    _log.Log(DateTimeOffset.UtcNow, "Information", $"Processed {getNextQueueMessageRequest.TypeId} ({(response.Response.ErrorCode == null ? "Success" : response.Response.ErrorMessage)})");
                }
            });
        }

        public bool CanProcess(MessageBase message)
        {
            return message.TypeId == MessageTypeIds.GetNextQueueMessageRequest;
        }
    }
}
