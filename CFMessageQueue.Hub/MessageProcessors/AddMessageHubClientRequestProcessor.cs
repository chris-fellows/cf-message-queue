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
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace CFMessageQueue.Hub.MessageProcessors
{
    internal class AddMessageHubClientRequestProcessor : MessageProcessorBase, IMessageProcessor
    {        
        public AddMessageHubClientRequestProcessor(IAuditLog auditLog, ISimpleLog log, IServiceProvider serviceProvider) : base(auditLog, log, serviceProvider)
        {

        }

        public Task ProcessAsync(MessageBase message, MessageReceivedInfo messageReceivedInfo)
        {
            return Task.Run(async () =>
            {
                var addMessageHubClientRequest = (AddMessageHubClientRequest)message;

                _log.Log(DateTimeOffset.UtcNow, "Information", $"Processing {addMessageHubClientRequest.TypeId}");

                var response = new AddMessageHubClientResponse()
                {
                    Response = new MessageResponse()
                    {
                        IsMore = false,
                        MessageId = addMessageHubClientRequest.Id,
                        Sequence = 1
                    },
                    MessageHubClientId = ""
                };

                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var messageHubService = scope.ServiceProvider.GetRequiredService<IQueueMessageHubService>();
                        var messageHubClientService = scope.ServiceProvider.GetRequiredService<IMessageHubClientService>();
                        var queueMessageHubService = scope.ServiceProvider.GetRequiredService<IQueueMessageHubService>();

                        var messageHubClient = GetMessageHubClientBySecurityKey(addMessageHubClientRequest.SecurityKey, messageHubClientService);

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
                        else if (String.IsNullOrEmpty(addMessageHubClientRequest.Name))
                        {
                            response.Response.ErrorCode = ResponseErrorCodes.InvalidParameters;
                            response.Response.ErrorMessage = EnumUtilities.GetEnumDescription(response.Response.ErrorCode);
                        }
                        else if (!IsValidFormatSecurityKey(addMessageHubClientRequest.ClientSecurityKey))
                        {
                            response.Response.ErrorCode = ResponseErrorCodes.InvalidParameters;
                            response.Response.ErrorMessage = $"{EnumUtilities.GetEnumDescription(response.Response.ErrorCode)}: Security Key is an invalid format";
                        }
                        else
                        {
                            // Check if security key is used already
                            var isMessageHubClientForKey = (await messageHubClientService.GetAllAsync())
                                                    .Any(c => c.SecurityKey.ToLower() == addMessageHubClientRequest.ClientSecurityKey.ToLower());

                            // Check if name is used already
                            var isMessageHubClientForName = (await messageHubClientService.GetAllAsync())
                                                   .Any(c => c.Name.ToLower() == addMessageHubClientRequest.Name.ToLower());

                            if (isMessageHubClientForKey)
                            {
                                response.Response.ErrorCode = ResponseErrorCodes.InvalidParameters;
                                response.Response.ErrorMessage = $"{EnumUtilities.GetEnumDescription(response.Response.ErrorCode)}: Security Key must be unique";
                            }
                            else if (isMessageHubClientForName)
                            {
                                response.Response.ErrorCode = ResponseErrorCodes.InvalidParameters;
                                response.Response.ErrorMessage = $"{EnumUtilities.GetEnumDescription(response.Response.ErrorCode)}: Name must be unique";
                            }
                            else
                            {
                                // Add message hub client
                                var newMessageHubClient = new MessageHubClient()
                                {
                                    Id = Guid.NewGuid().ToString(),
                                    Name = addMessageHubClientRequest.Name,
                                    SecurityKey = addMessageHubClientRequest.ClientSecurityKey
                                };
                                await messageHubClientService.AddAsync(newMessageHubClient);

                                // Update memory cache of message hub clients
                                _hubResources.MessageHubClientsBySecurityKey.Add(newMessageHubClient.SecurityKey, messageHubClient);

                                // Add default permissions so that client can see message hubs & message queues. They will just be able to
                                // see the hubs & queues but not modify them
                                var queueMessageHub = await queueMessageHubService.GetByIdAsync(_hubResources.QueueMessageHub.Id);
                                queueMessageHub.SecurityItems.Add(new SecurityItem()
                                {
                                    Id = Guid.NewGuid().ToString(),
                                    MessageHubClientId = newMessageHubClient.Id,
                                    RoleTypes = RoleTypeUtilities.DefaultNonAdminHubClientRoleTypes
                                });
                                await queueMessageHubService.UpdateAsync(queueMessageHub);

                                _hubResources.QueueMessageHub = queueMessageHub;

                                response.MessageHubClientId = newMessageHubClient.Id;
                            }
                        }
                    }
                }
                catch (Exception exception)
                {
                    response.Response.ErrorCode = ResponseErrorCodes.Unknown;
                    response.Response.ErrorMessage = $"{EnumUtilities.GetEnumDescription(response.Response.ErrorCode)}: {exception.Message}";

                    _log.Log(DateTimeOffset.UtcNow, "Error", $"Error processing {addMessageHubClientRequest.TypeId}: {exception.Message}");
                }
                finally
                {
                    // Send response
                    _hubResources.ClientsConnection.SendMessage(response, messageReceivedInfo.RemoteEndpointInfo);

                    _log.Log(DateTimeOffset.UtcNow, "Information", $"Processed {addMessageHubClientRequest.TypeId} ({(response.Response.ErrorCode == null ? "Success" : response.Response.ErrorMessage)})");
                }
            });
        }

        public bool CanProcess(MessageBase message)
        {
            return message.TypeId == MessageTypeIds.AddMessageHubClientRequest;
        }
    }
}
