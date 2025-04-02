using CFConnectionMessaging.Interfaces;
using CFMessageQueue.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CFMessageQueue
{
    public class MessageConverterList
    {
        private readonly IExternalMessageConverter<AddQueueMessageRequest> _addQueueMessageRequestConverter;
        private readonly IExternalMessageConverter<AddQueueMessageResponse> _addQueueMessageResponseConverter;

        private readonly IExternalMessageConverter<GetMessageHubsRequest> _getMessageHubsRequestConverter;
        private readonly IExternalMessageConverter<GetMessageHubsResponse> _getMessageHubsResponseConverter;
        
        private readonly IExternalMessageConverter<GetNextQueueMessageRequest> _getNextQueueMessageRequestConverter;
        private readonly IExternalMessageConverter<GetNextQueueMessageResponse> _getNextQueueMessageResponseConverter;

        private readonly IExternalMessageConverter<MessageQueueSubscribeRequest> _messageQueueSubscribeRequestConverter;
        private readonly IExternalMessageConverter<MessageQueueSubscribeResponse> _messageQueueSubscribeResponseConverter;


        public IExternalMessageConverter<AddQueueMessageRequest> AddQueueMessageRequestConverter => _addQueueMessageRequestConverter;
        public IExternalMessageConverter<AddQueueMessageResponse> AddQueueMessageResponseConverter => _addQueueMessageResponseConverter;
        
        public IExternalMessageConverter<GetMessageHubsRequest> GetMessageHubsRequestConverter => _getMessageHubsRequestConverter;
        public IExternalMessageConverter<GetMessageHubsResponse> GetMessageHubsResponseConverter => _getMessageHubsResponseConverter;

        public IExternalMessageConverter<GetNextQueueMessageRequest> GetNextQueueMessageRequestConverter => _getNextQueueMessageRequestConverter;
        public IExternalMessageConverter<GetNextQueueMessageResponse> GetNextQueueMessageResponseConverter => _getNextQueueMessageResponseConverter;

        public IExternalMessageConverter<MessageQueueSubscribeRequest> MessageQueueSubscribeRequestConverter => _messageQueueSubscribeRequestConverter;
        public IExternalMessageConverter<MessageQueueSubscribeResponse> MessageQueueSubscribeResponseConverter => _addQueueMessageResponseConverter;
    }
}
