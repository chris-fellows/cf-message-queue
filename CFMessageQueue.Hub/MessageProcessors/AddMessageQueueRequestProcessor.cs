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
    internal class AddMessageQueueRequestProcessor : MessageProcessorBase, IMessageProcessor
    {
        public AddMessageQueueRequestProcessor(IAuditLog auditLog, ISimpleLog log, IServiceProvider serviceProvider) : base(auditLog, log, serviceProvider)
        {

        }

        public Task ProcessAsync(MessageBase message, MessageReceivedInfo messageReceivedInfo)
        {
            return Task.Run(async () =>
            {
                var addMessageQueueRequest = (AddMessageQueueRequest)message;

                _log.Log(DateTimeOffset.UtcNow, "Information", $"Processing {addMessageQueueRequest.TypeId}");

                var response = new AddMessageQueueResponse()
                {
                    Response = new MessageResponse()
                    {
                        IsMore = false,
                        MessageId = addMessageQueueRequest.Id,
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
                        var queueMessageHubService = scope.ServiceProvider.GetRequiredService<IQueueMessageHubService>();

                        var messageHubClient = GetMessageHubClientBySecurityKey(addMessageQueueRequest.SecurityKey, messageHubClientService);

                        var securityItem = messageHubClient == null ? null : _hubResources.QueueMessageHub.SecurityItems.FirstOrDefault(si => si.MessageHubClientId == messageHubClient.Id);

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
                                    var messageHub = await messageHubService.GetByIdAsync(_hubResources.QueueMessageHub.Id);
                                    foreach (var currentSecurityItem in messageHub.SecurityItems.Where(si => si.RoleTypes.Contains(RoleTypes.HubAdmin)))
                                    {
                                        messageQueue.SecurityItems.Add(new SecurityItem()
                                        {
                                            Id = Guid.NewGuid().ToString(),
                                            MessageHubClientId = currentSecurityItem.MessageHubClientId,
                                            RoleTypes = RoleTypeUtilities.DefaultAdminQueueClientRoleTypes
                                        });
                                    }

                                    await messageQueueService.AddAsync(messageQueue);

                                    response.MessageQueueId = messageQueue.Id;

                                    // Add message queue worker
                                    var messageQueueWorker = new MessageQueueWorker(messageQueue, _serviceProvider, _hubResources.SystemConfig);
                                    messageQueueWorker.Start();
                                    _hubResources.MessageQueueWorkers.Add(messageQueueWorker);

                                    // Log queue created
                                    _auditLog.LogQueue(DateTimeOffset.UtcNow, "QUEUE_CREATED", messageQueue.Name);
                                }
                            }
                        }
                    }
                }
                catch (Exception exception)
                {
                    response.Response.ErrorCode = ResponseErrorCodes.Unknown;
                    response.Response.ErrorMessage = $"{EnumUtilities.GetEnumDescription(response.Response.ErrorCode)}: {exception.Message}";

                    _log.Log(DateTimeOffset.UtcNow, "Error", $"Error processing {addMessageQueueRequest.TypeId}: {exception.Message}");
                }
                finally
                {
                    // Send response
                    _hubResources.ClientsConnection.SendMessage(response, messageReceivedInfo.RemoteEndpointInfo);

                    _log.Log(DateTimeOffset.UtcNow, "Information", $"Processed {addMessageQueueRequest.TypeId} ({(response.Response.ErrorCode == null ? "Success" : response.Response.ErrorMessage)})");
                }
            });
        }

        public bool CanProcess(MessageBase message)
        {
            return message.TypeId == MessageTypeIds.AddMessageQueueRequest;
        }
    }
}
