using CFConnectionMessaging.Interfaces;
using CFConnectionMessaging.Models;
using CFMessageQueue.Models;
using CFMessageQueue.MessageConverters;
using CFMessageQueue.Constants;

namespace CFMessageQueue
{
    /// <summary>
    /// Converters for connection messages (TCP) between internal (ConnectionMessage) and external (Our) format
    /// </summary>
    public class MessageConverterList
    {
        private readonly IExternalMessageConverter<AddMessageHubClientRequest> _addMessageHubClientRequestConverter = new AddMessageHubClientRequestConverter();
        private readonly IExternalMessageConverter<AddMessageHubClientResponse> _addMessageHubClientResponseConverter = new AddMessageHubClientResponseConverter();

        private readonly IExternalMessageConverter<AddMessageQueueRequest> _addMessageQueueRequestConverter = new AddMessageQueueRequestConverter();
        private readonly IExternalMessageConverter<AddMessageQueueResponse> _addMessageQueueResponseConverter = new AddMessageQueueResponseConverter();

        private readonly IExternalMessageConverter<AddQueueMessageRequest> _addQueueMessageRequestConverter = new AddQueueMessageRequestConverter();
        private readonly IExternalMessageConverter<AddQueueMessageResponse> _addQueueMessageResponseConverter = new AddQueueMessageResponseConverter();

        private readonly IExternalMessageConverter<ConfigureMessageHubClientRequest> _configureMessageHubClientRequestConverter = new ConfigureMessageHubClientRequestConverter();
        private readonly IExternalMessageConverter<ConfigureMessageHubClientResponse> _configureMessageHubClientResponseConverter = new ConfigureMessageHubClientResponseConverter();

        private readonly IExternalMessageConverter<ExecuteMessageQueueActionRequest> _executeMessageQueueActionRequestConverter = new ExecuteMessageQueueActionRequestConverter();
        private readonly IExternalMessageConverter<ExecuteMessageQueueActionResponse> _executeMessageQueueActionResponseConverter = new ExecuteMessageQueueActionResponseConverter();

        private readonly IExternalMessageConverter<GetMessageHubClientsRequest> _getMessageHubClientsRequestConverter = new GetMessageHubClientsRequestConverter();
        private readonly IExternalMessageConverter<GetMessageHubClientsResponse> _getMessageHubClientsResponseConverter = new GetMessageHubClientsResponseConverter();

        private readonly IExternalMessageConverter<GetMessageHubsRequest> _getMessageHubsRequestConverter= new GetMessageHubsRequestConverter();
        private readonly IExternalMessageConverter<GetMessageHubsResponse> _getMessageHubsResponseConverter = new GetMessageHubsResponseConverter();

        private readonly IExternalMessageConverter<GetMessageQueuesRequest> _getMessageQueuesRequestConverter = new GetMessageQueuesRequestConverter();
        private readonly IExternalMessageConverter<GetMessageQueuesResponse> _getMessageQueuesResponseConverter = new GetMessageQueuesResponseConverter();

        private readonly IExternalMessageConverter<GetNextQueueMessageRequest> _getNextQueueMessageRequestConverter = new GetNextQueueMessageRequestConverter();
        private readonly IExternalMessageConverter<GetNextQueueMessageResponse> _getNextQueueMessageResponseConverter = new GetNextQueueMessageResponseConverter();

        private readonly IExternalMessageConverter<GetQueueMessagesRequest> _getQueueMessagesRequestConverter = new GetQueueMessagesRequestConverter();
        private readonly IExternalMessageConverter<GetQueueMessagesResponse> _getQueueMessagesResponseConverter = new GetQueueMessagesResponseConverter();

        private readonly IExternalMessageConverter<MessageQueueNotificationMessage> _messageQueueNotificationMessageConverter = new MessageQueueNotificationMessageConverter();

        private readonly IExternalMessageConverter<MessageQueueSubscribeRequest> _messageQueueSubscribeRequestConverter = new MessageQueueSubscribeRequestConverter();
        private readonly IExternalMessageConverter<MessageQueueSubscribeResponse> _messageQueueSubscribeResponseConverter = new MessageQueueSubscribeResponseConverter();

        private readonly IExternalMessageConverter<QueueMessageProcessedRequest> _queueMessageProcessedRequestConverter = new QueueMessageProcessedRequestConverter();
        private readonly IExternalMessageConverter<QueueMessageProcessedResponse> _queueMessageProcessedResponseConverter = new QueueMessageProcessedResponseConverter();

        public ConnectionMessage GetConnectionMessage(MessageBase externalMessage)
        {
            return externalMessage.TypeId switch
            {
                MessageTypeIds.AddMessageHubClientRequest => _addMessageHubClientRequestConverter.GetConnectionMessage((AddMessageHubClientRequest)externalMessage),
                MessageTypeIds.AddMessageHubClientResponse => _addMessageHubClientResponseConverter.GetConnectionMessage((AddMessageHubClientResponse)externalMessage),

                MessageTypeIds.AddMessageQueueRequest => _addMessageQueueRequestConverter.GetConnectionMessage((AddMessageQueueRequest)externalMessage),
                MessageTypeIds.AddMessageQueueResponse => _addMessageQueueResponseConverter.GetConnectionMessage((AddMessageQueueResponse)externalMessage),

                MessageTypeIds.AddQueueMessageRequest => _addQueueMessageRequestConverter.GetConnectionMessage((AddQueueMessageRequest)externalMessage),
                MessageTypeIds.AddQueueMessageResponse => _addQueueMessageResponseConverter.GetConnectionMessage((AddQueueMessageResponse)externalMessage),

                MessageTypeIds.ConfigureMessageHubClientRequest => _configureMessageHubClientRequestConverter.GetConnectionMessage((ConfigureMessageHubClientRequest)externalMessage),
                MessageTypeIds.ConfigureMessageHubClientResponse => _configureMessageHubClientResponseConverter.GetConnectionMessage((ConfigureMessageHubClientResponse)externalMessage),

                MessageTypeIds.ExecuteMessageQueueActionRequest => _executeMessageQueueActionRequestConverter.GetConnectionMessage((ExecuteMessageQueueActionRequest)externalMessage),
                MessageTypeIds.ExecuteMessageQueueActionResponse => _executeMessageQueueActionResponseConverter.GetConnectionMessage((ExecuteMessageQueueActionResponse)externalMessage),

                MessageTypeIds.GetMessageHubClientsRequest => _getMessageHubClientsRequestConverter.GetConnectionMessage((GetMessageHubClientsRequest)externalMessage),
                MessageTypeIds.GetMessageHubClientsResponse => _getMessageHubClientsResponseConverter.GetConnectionMessage((GetMessageHubClientsResponse)externalMessage),

                MessageTypeIds.GetMessageHubsRequest => _getMessageHubsRequestConverter.GetConnectionMessage((GetMessageHubsRequest)externalMessage),
                MessageTypeIds.GetMessageHubsResponse => _getMessageHubsResponseConverter.GetConnectionMessage((GetMessageHubsResponse)externalMessage),

                MessageTypeIds.GetMessageQueuesRequest => _getMessageQueuesRequestConverter.GetConnectionMessage((GetMessageQueuesRequest)externalMessage),
                MessageTypeIds.GetMessageQueuesResponse => _getMessageQueuesResponseConverter.GetConnectionMessage((GetMessageQueuesResponse)externalMessage),

                MessageTypeIds.GetNextQueueMessageRequest => _getNextQueueMessageRequestConverter.GetConnectionMessage((GetNextQueueMessageRequest)externalMessage),
                MessageTypeIds.GetNextQueueMessageResponse => _getNextQueueMessageResponseConverter.GetConnectionMessage((GetNextQueueMessageResponse)externalMessage),

                MessageTypeIds.GetQueueMessagesRequest => _getQueueMessagesRequestConverter.GetConnectionMessage((GetQueueMessagesRequest)externalMessage),
                MessageTypeIds.GetQueueMessagesResponse => _getQueueMessagesResponseConverter.GetConnectionMessage((GetQueueMessagesResponse)externalMessage),

                MessageTypeIds.MessageQueueNotification => _messageQueueNotificationMessageConverter.GetConnectionMessage((MessageQueueNotificationMessage)externalMessage),

                MessageTypeIds.MessageQueueSubscribeRequest => _messageQueueSubscribeRequestConverter.GetConnectionMessage((MessageQueueSubscribeRequest)externalMessage),
                MessageTypeIds.MessageQueueSubscribeResponse => _messageQueueSubscribeResponseConverter.GetConnectionMessage((MessageQueueSubscribeResponse)externalMessage),

                MessageTypeIds.QueueMessageProcessedRequest => _queueMessageProcessedRequestConverter.GetConnectionMessage((QueueMessageProcessedRequest)externalMessage),
                MessageTypeIds.QueueMessageProcessedResponse => _queueMessageProcessedResponseConverter.GetConnectionMessage((QueueMessageProcessedResponse)externalMessage),

                _ => throw new ArgumentException("Cannot convert external message to connection message")
            };
        }

        public MessageBase GetExternalMessage(ConnectionMessage connectionMessage)
        {
            return connectionMessage.TypeId switch
            {
                MessageTypeIds.AddMessageHubClientRequest => _addMessageHubClientRequestConverter.GetExternalMessage(connectionMessage),
                MessageTypeIds.AddMessageHubClientResponse => _addMessageHubClientResponseConverter.GetExternalMessage(connectionMessage),

                MessageTypeIds.AddMessageQueueRequest => _addMessageQueueRequestConverter.GetExternalMessage(connectionMessage),
                MessageTypeIds.AddMessageQueueResponse => _addMessageQueueResponseConverter.GetExternalMessage(connectionMessage),

                MessageTypeIds.AddQueueMessageRequest => _addQueueMessageRequestConverter.GetExternalMessage(connectionMessage),
                MessageTypeIds.AddQueueMessageResponse => _addQueueMessageResponseConverter.GetExternalMessage(connectionMessage),

                MessageTypeIds.ConfigureMessageHubClientRequest => _configureMessageHubClientRequestConverter.GetExternalMessage(connectionMessage),
                MessageTypeIds.ConfigureMessageHubClientResponse => _configureMessageHubClientResponseConverter.GetExternalMessage(connectionMessage),

                MessageTypeIds.ExecuteMessageQueueActionRequest => _executeMessageQueueActionRequestConverter.GetExternalMessage(connectionMessage),
                MessageTypeIds.ExecuteMessageQueueActionResponse => _executeMessageQueueActionResponseConverter.GetExternalMessage(connectionMessage),

                MessageTypeIds.GetMessageHubClientsRequest => _getMessageHubClientsRequestConverter.GetExternalMessage(connectionMessage),
                MessageTypeIds.GetMessageHubClientsResponse => _getMessageHubClientsResponseConverter.GetExternalMessage(connectionMessage),

                MessageTypeIds.GetMessageHubsRequest => _getMessageHubsRequestConverter.GetExternalMessage(connectionMessage),
                MessageTypeIds.GetMessageHubsResponse => _getMessageHubsResponseConverter.GetExternalMessage(connectionMessage),

                MessageTypeIds.GetMessageQueuesRequest => _getMessageQueuesRequestConverter.GetExternalMessage(connectionMessage),
                MessageTypeIds.GetMessageQueuesResponse => _getMessageQueuesResponseConverter.GetExternalMessage(connectionMessage),

                MessageTypeIds.GetNextQueueMessageRequest => _getNextQueueMessageRequestConverter.GetExternalMessage(connectionMessage),
                MessageTypeIds.GetNextQueueMessageResponse => _getNextQueueMessageResponseConverter.GetExternalMessage(connectionMessage),

                MessageTypeIds.GetQueueMessagesRequest => _getQueueMessagesRequestConverter.GetExternalMessage(connectionMessage),
                MessageTypeIds.GetQueueMessagesResponse => _getQueueMessagesResponseConverter.GetExternalMessage(connectionMessage),

                MessageTypeIds.MessageQueueNotification => _messageQueueNotificationMessageConverter.GetExternalMessage(connectionMessage),

                MessageTypeIds.MessageQueueSubscribeRequest => _messageQueueSubscribeRequestConverter.GetExternalMessage(connectionMessage),
                MessageTypeIds.MessageQueueSubscribeResponse => _messageQueueSubscribeResponseConverter.GetExternalMessage(connectionMessage),

                MessageTypeIds.QueueMessageProcessedRequest => _queueMessageProcessedRequestConverter.GetExternalMessage(connectionMessage),
                MessageTypeIds.QueueMessageProcessedResponse => _queueMessageProcessedResponseConverter.GetExternalMessage(connectionMessage),

                _ => throw new ArgumentException("Cannot convert external message to connection message")
            };
        }
    }
}
