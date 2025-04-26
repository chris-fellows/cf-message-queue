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
    internal class GetMessageQueuesRequestProcessor : MessageProcessorBase, IMessageProcessor
    {
        public GetMessageQueuesRequestProcessor(IAuditLog auditLog, ISimpleLog log, IServiceProvider serviceProvider) : base(auditLog, log, serviceProvider)
        {

        }

        public Task ProcessAsync(MessageBase message, MessageReceivedInfo messageReceivedInfo)
        {
            return Task.Run(async () =>
            {
                var getMessageQueuesRequest = (GetMessageQueuesRequest)message;

                _log.Log(DateTimeOffset.UtcNow, "Information", $"Processing {getMessageQueuesRequest.TypeId} (Security Key={getMessageQueuesRequest.SecurityKey})");

                var response = new GetMessageQueuesResponse()
                {
                    Response = new MessageResponse()
                    {
                        IsMore = false,
                        MessageId = getMessageQueuesRequest.Id,
                        Sequence = 1
                    },
                    MessageQueues = new()
                };

                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var messageHubClientService = scope.ServiceProvider.GetRequiredService<IMessageHubClientService>();
                        var messageQueueService = scope.ServiceProvider.GetRequiredService<IMessageQueueService>();

                        var messageHubClient = GetMessageHubClientBySecurityKey(getMessageQueuesRequest.SecurityKey, messageHubClientService);

                        var securityItem = messageHubClient == null ? null : _hubResources.QueueMessageHub.SecurityItems.FirstOrDefault(si => si.MessageHubClientId == messageHubClient.Id);

                        if (messageHubClient == null)
                        {
                            response.Response.ErrorCode = ResponseErrorCodes.PermissionDenied;
                            response.Response.ErrorMessage = EnumUtilities.GetEnumDescription(response.Response.ErrorCode);
                        }
                        else if (securityItem == null || !securityItem.RoleTypes.Contains(RoleTypes.HubReadMessageQueues))
                        {
                            response.Response.ErrorCode = ResponseErrorCodes.PermissionDenied;
                            response.Response.ErrorMessage = EnumUtilities.GetEnumDescription(response.Response.ErrorCode);
                        }
                        else
                        {
                            // TODO: Consider filtering which queues are visible. Could check messageHubClient.SecurityItems
                            var messageQueues = await messageQueueService.GetAllAsync();
                            response.MessageQueues = messageQueues;
                        }
                    }
                }
                catch (Exception exception)
                {
                    response.Response.ErrorCode = ResponseErrorCodes.Unknown;
                    response.Response.ErrorMessage = $"{EnumUtilities.GetEnumDescription(response.Response.ErrorCode)}: {exception.Message}";

                    _log.Log(DateTimeOffset.UtcNow, "Error", $"Error processing {getMessageQueuesRequest.TypeId}: {exception.Message}");
                }
                finally
                {
                    // Send response
                    _hubResources.ClientsConnection.SendMessage(response, messageReceivedInfo.RemoteEndpointInfo);

                    _log.Log(DateTimeOffset.UtcNow, "Information", $"Processed {getMessageQueuesRequest.TypeId} ({(response.Response.ErrorCode == null ? "Success" : response.Response.ErrorMessage)})");
                }
            });
        }

        public bool CanProcess(MessageBase message)
        {
            return message.TypeId == MessageTypeIds.GetMessageQueuesRequest;
        }
    }
}
