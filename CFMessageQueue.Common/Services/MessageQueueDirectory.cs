using CFConnectionMessaging.Models;
using CFMessageQueue.Common.Models;
using CFMessageQueue.Exceptions;
using CFMessageQueue.Interfaces;
using CFMessageQueue.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CFMessageQueue.Services
{
    public class MessageQueueDirectory : IMessageQueueDirectory
    {
        private readonly MessageHubConnection _messageHubConnection;

        private EndpointInfo _defaultMessageHubEndpointInfo;

        public Task<List<MessageHub>> GetMessageHubsAsync()
        {
            var getMessageHubsRequest = new GetMessageHubsRequest()
            {

            };
            
            try
            {
                var response = _messageHubConnection.SendGetMessageHubsRequest(getMessageHubsRequest, _defaultMessageHubEndpointInfo);
            }
            catch (MessageConnectionException messageConnectionException)
            {
                throw new MessageQueueException("Error getting message hubs", messageConnectionException);
            }
        }
    }
}
