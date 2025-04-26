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
    internal class GetMessageHubClientsRequestProcessor : MessageProcessorBase, IMessageProcessor
    {
        public GetMessageHubClientsRequestProcessor(IAuditLog auditLog, ISimpleLog log, IServiceProvider serviceProvider) : base(auditLog, log, serviceProvider)
        {

        }

        public Task ProcessAsync(MessageBase message, MessageReceivedInfo messageReceivedInfo)
        {
            return Task.Run(async () =>
            {
                var getMessageHubClientsRequest = (GetMessageHubClientsRequest)message;

                _log.Log(DateTimeOffset.UtcNow, "Information", $"Processing {getMessageHubClientsRequest.TypeId}");

                var response = new GetMessageHubClientsResponse()
                {
                    Response = new MessageResponse()
                    {
                        IsMore = false,
                        MessageId = getMessageHubClientsRequest.Id,
                        Sequence = 1
                    },
                    MessageHubClients = new()
                };

                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var messageHubClientService = scope.ServiceProvider.GetService<IMessageHubClientService>();
                        var queueMessageHubService = scope.ServiceProvider.GetRequiredService<IQueueMessageHubService>();

                        var messageHubClient = GetMessageHubClientBySecurityKey(getMessageHubClientsRequest.SecurityKey, messageHubClientService);

                        var securityItem = messageHubClient == null ? null : _hubResources.QueueMessageHub.SecurityItems.FirstOrDefault(si => si.MessageHubClientId == messageHubClient.Id);

                        if (messageHubClient == null)
                        {
                            response.Response.ErrorCode = ResponseErrorCodes.PermissionDenied;
                            response.Response.ErrorMessage = EnumUtilities.GetEnumDescription(response.Response.ErrorCode);
                        }
                        else if (securityItem == null || !securityItem.RoleTypes.Contains(RoleTypes.HubReadMessageHubClients))
                        {
                            response.Response.ErrorCode = ResponseErrorCodes.PermissionDenied;
                            response.Response.ErrorMessage = EnumUtilities.GetEnumDescription(response.Response.ErrorCode);
                        }
                        else
                        {
                            var messageHubClients = await messageHubClientService.GetAllAsync();
                            response.MessageHubClients = messageHubClients;
                        }
                    }
                }
                catch (Exception exception)
                {
                    response.Response.ErrorCode = ResponseErrorCodes.Unknown;
                    response.Response.ErrorMessage = $"{EnumUtilities.GetEnumDescription(response.Response.ErrorCode)}: {exception.Message}";

                    _log.Log(DateTimeOffset.UtcNow, "Error", $"Error processing {getMessageHubClientsRequest.TypeId}: {exception.Message}");
                }
                finally
                {
                    // Send response
                    _hubResources.ClientsConnection.SendMessage(response, messageReceivedInfo.RemoteEndpointInfo);

                    _log.Log(DateTimeOffset.UtcNow, "Information", $"Processed {getMessageHubClientsRequest.TypeId} ({(response.Response.ErrorCode == null ? "Success" : response.Response.ErrorMessage)})");
                }
            });
        }

        public bool CanProcess(MessageBase message)
        {
            return message.TypeId == MessageTypeIds.GetMessageHubClientsRequest;
        }
    }
}
