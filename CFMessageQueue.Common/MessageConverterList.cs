using CFConnectionMessaging.Interfaces;
using CFMessageQueue.Models;
using CFMessageQueue.MessageConverters;

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

        private readonly IExternalMessageConverter<QueueMessageProcessedMessage> _queueMessageProcessedMessageConverter = new QueueMessageProcessedMessageConverter();

        public IExternalMessageConverter<AddMessageHubClientRequest> AddMessageHubClientRequestConverter => _addMessageHubClientRequestConverter;
        public IExternalMessageConverter<AddMessageHubClientResponse> AddMessageHubClientResponseConverter => _addMessageHubClientResponseConverter;

        public IExternalMessageConverter<AddMessageQueueRequest> AddMessageQueueRequestConverter => _addMessageQueueRequestConverter;
        public IExternalMessageConverter<AddMessageQueueResponse> AddMessageQueueResponseConverter => _addMessageQueueResponseConverter;

        public IExternalMessageConverter<AddQueueMessageRequest> AddQueueMessageRequestConverter => _addQueueMessageRequestConverter;
        public IExternalMessageConverter<AddQueueMessageResponse> AddQueueMessageResponseConverter => _addQueueMessageResponseConverter;

        public IExternalMessageConverter<ConfigureMessageHubClientRequest> ConfigureMessageHubClientRequestConverter => _configureMessageHubClientRequestConverter;
        public IExternalMessageConverter<ConfigureMessageHubClientResponse> ConfigureMessageHubClientResponseConverter => _configureMessageHubClientResponseConverter;

        public IExternalMessageConverter<ExecuteMessageQueueActionRequest> ExecuteMessageQueueActionRequestConverter => _executeMessageQueueActionRequestConverter;
        public IExternalMessageConverter<ExecuteMessageQueueActionResponse> ExecuteMessageQueueActionResponseConverter => _executeMessageQueueActionResponseConverter;

        public IExternalMessageConverter<GetMessageHubClientsRequest> GetMessageHubClientsRequestConverter => _getMessageHubClientsRequestConverter;
        public IExternalMessageConverter<GetMessageHubClientsResponse> GetMessageHubClientsResponseConverter => _getMessageHubClientsResponseConverter;

        public IExternalMessageConverter<GetMessageHubsRequest> GetMessageHubsRequestConverter => _getMessageHubsRequestConverter;
        public IExternalMessageConverter<GetMessageHubsResponse> GetMessageHubsResponseConverter => _getMessageHubsResponseConverter;

        public IExternalMessageConverter<GetMessageQueuesRequest> GetMessageQueuesRequestConverter => _getMessageQueuesRequestConverter;
        public IExternalMessageConverter<GetMessageQueuesResponse> GetMessageQueuesResponseConverter => _getMessageQueuesResponseConverter;

        public IExternalMessageConverter<GetNextQueueMessageRequest> GetNextQueueMessageRequestConverter => _getNextQueueMessageRequestConverter;
        public IExternalMessageConverter<GetNextQueueMessageResponse> GetNextQueueMessageResponseConverter => _getNextQueueMessageResponseConverter;

        public IExternalMessageConverter<GetQueueMessagesRequest> GetQueueMessagesRequestConverter => _getQueueMessagesRequestConverter;
        public IExternalMessageConverter<GetQueueMessagesResponse> GetQueueMessagesResponseConverter => _getQueueMessagesResponseConverter;

        public IExternalMessageConverter<MessageQueueNotificationMessage> MessageQueueNotificationMessageConverter => _messageQueueNotificationMessageConverter;

        public IExternalMessageConverter<MessageQueueSubscribeRequest> MessageQueueSubscribeRequestConverter => _messageQueueSubscribeRequestConverter;
        public IExternalMessageConverter<MessageQueueSubscribeResponse> MessageQueueSubscribeResponseConverter => _messageQueueSubscribeResponseConverter;

        public IExternalMessageConverter<QueueMessageProcessedMessage> QueueMessageProcessedMessageConverter => _queueMessageProcessedMessageConverter;
    }
}
