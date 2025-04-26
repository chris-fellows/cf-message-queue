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
    internal class ConfigureMessageHubClientRequestProcessor : MessageProcessorBase, IMessageProcessor
    {
        public ConfigureMessageHubClientRequestProcessor(IAuditLog auditLog, ISimpleLog log, IServiceProvider serviceProvider) : base(auditLog, log, serviceProvider)
        {

        }

        public Task ProcessAsync(MessageBase message, MessageReceivedInfo messageReceivedInfo)
        {
            return Task.Run(async () =>
            {
                var configureMessageHubClientRequest = (ConfigureMessageHubClientRequest)message;

                _log.Log(DateTimeOffset.UtcNow, "Information", $"Processing {configureMessageHubClientRequest.TypeId}");

                var response = new ConfigureMessageHubClientResponse()
                {
                    Response = new MessageResponse()
                    {
                        IsMore = false,
                        MessageId = configureMessageHubClientRequest.Id,
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

                        var messageHubClient = GetMessageHubClientBySecurityKey(configureMessageHubClientRequest.SecurityKey, messageHubClientService);

                        var messageHubClientEdit = await messageHubClientService.GetByIdAsync(configureMessageHubClientRequest.MessageHubClientId);

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
                        else if (messageHubClientEdit == null)
                        {
                            response.Response.ErrorCode = ResponseErrorCodes.InvalidParameters;
                            response.Response.ErrorMessage = $"{EnumUtilities.GetEnumDescription(response.Response.ErrorCode)}: Message Hub Client Id is invalid";
                        }
                        else if (String.IsNullOrEmpty(configureMessageHubClientRequest.MessageQueueId) &&
                                configureMessageHubClientRequest.RoleTypes.Except(RoleTypeUtilities.HubRoleTypes).Any())
                        {
                            response.Response.ErrorCode = ResponseErrorCodes.InvalidParameters;
                            response.Response.ErrorMessage = $"{EnumUtilities.GetEnumDescription(response.Response.ErrorCode)}: Some role types are not valid for the hub";
                        }
                        else if (!String.IsNullOrEmpty(configureMessageHubClientRequest.MessageQueueId) &&
                                configureMessageHubClientRequest.RoleTypes.Except(RoleTypeUtilities.QueueRoleTypes).Any())
                        {
                            response.Response.ErrorCode = ResponseErrorCodes.InvalidParameters;
                            response.Response.ErrorMessage = $"{EnumUtilities.GetEnumDescription(response.Response.ErrorCode)}: Some role types are not valid for queues";
                        }
                        else
                        {
                            if (String.IsNullOrEmpty(configureMessageHubClientRequest.MessageQueueId))   // Hub config
                            {
                                var queueMessageHub = await queueMessageHubService.GetByIdAsync(_hubResources.QueueMessageHub.Id);

                                // Update SecurityItem.RoleTypes
                                var securityItemEdit = queueMessageHub.SecurityItems.FirstOrDefault(si => si.MessageHubClientId == messageHubClientEdit.Id);
                                if (securityItemEdit == null)
                                {
                                    securityItemEdit = new SecurityItem()
                                    {
                                        Id = Guid.NewGuid().ToString(),
                                        MessageHubClientId = messageHubClientEdit.Id,
                                        RoleTypes = configureMessageHubClientRequest.RoleTypes
                                    };
                                    queueMessageHub.SecurityItems.Add(securityItemEdit);
                                }
                                else
                                {
                                    securityItemEdit.RoleTypes = configureMessageHubClientRequest.RoleTypes;
                                }

                                // Remove security item if no roles
                                if (!securityItemEdit.RoleTypes.Any())
                                {
                                    queueMessageHub.SecurityItems.Remove(securityItemEdit);
                                }

                                await queueMessageHubService.UpdateAsync(queueMessageHub);

                                _hubResources.QueueMessageHub = queueMessageHub;
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
                                            Id = Guid.NewGuid().ToString(),
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
                    }
                }
                catch (Exception exception)
                {
                    response.Response.ErrorCode = ResponseErrorCodes.Unknown;
                    response.Response.ErrorMessage = $"{EnumUtilities.GetEnumDescription(response.Response.ErrorCode)}: {exception.Message}";

                    _log.Log(DateTimeOffset.UtcNow, "Error", $"Error processing {configureMessageHubClientRequest.TypeId}: {exception.Message}");
                }
                finally
                {
                    // Send response
                    _hubResources.ClientsConnection.SendMessage(response, messageReceivedInfo.RemoteEndpointInfo);

                    _log.Log(DateTimeOffset.UtcNow, "Information", $"Processed {configureMessageHubClientRequest.TypeId} ({(response.Response.ErrorCode == null ? "Success" : response.Response.ErrorMessage)})");
                }
            });
        }

        public bool CanProcess(MessageBase message)
        {
            return message.TypeId == MessageTypeIds.ConfigureMessageHubClientRequest;
        }
    }
}
