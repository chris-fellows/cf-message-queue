using CFConnectionMessaging.Models;
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
    public class MessageQueueClientService : IMessageQueueClientConnector
    {
        private MessageHubConnection _messageHubConnection = new MessageHubConnection();

        private MessageQueue? _messageQueue;

        private readonly string _securityKey;

        public MessageQueueClientService(string securityKey)
        {
            _securityKey = securityKey;
        }

        public void SetMessageQueue(MessageQueue messageQueue)
        {
            _messageQueue = messageQueue;
            _messageHubConnection = new MessageHubConnection();            
        }

        public async Task SendAsync(QueueMessage queueMessage)
        {
            var addQueueMessageRequest = new AddQueueMessageRequest()
            {
                SecurityKey = _securityKey,
                QueueMessage = queueMessage,
                MessageQueueId = _messageQueue.Id
            };

            var remoteEndpointInfo = new EndpointInfo()
            {
                Ip = _messageQueue.Ip,
                Port = _messageQueue.Port
            };

            try
            {
                var response = _messageHubConnection.SendAddQueueMessageMessage(addQueueMessageRequest, remoteEndpointInfo);
                ThrowResponseExceptionIfRequired(response);
            }
            catch(MessageConnectionException messageConnectionException)
            {
                throw new MessageQueueException("Error adding message to queue", messageConnectionException);
            }
        }

        public async Task<QueueMessage?> GetNextAsync()
        {
            var getNextQueueMessageRequest = new GetNextQueueMessageRequest()
            {
                SecurityKey = _securityKey,
                MessageQueueId = _messageQueue.Id
            };

            var remoteEndpointInfo = new EndpointInfo()
            {
                Ip = _messageQueue.Ip,
                Port = _messageQueue.Port
            };

            try
            {
                var response = _messageHubConnection.SendGetNextQueueMessageRequest(getNextQueueMessageRequest, remoteEndpointInfo);
                ThrowResponseExceptionIfRequired(response);

                return response.QueueMessage;
            }
            catch (MessageConnectionException messageConnectionException)
            {
                throw new MessageQueueException("Error getting next queue message", messageConnectionException);
            }
        }

        public async Task<string?> SubscribeAsync()
        {
            var messageQueueSubscribeRequest = new MessageQueueSubscribeRequest()
            {
                SecurityKey = _securityKey,
                MessageQueueId = _messageQueue.Id
            };

            var remoteEndpointInfo = new EndpointInfo()
            {
                Ip = _messageQueue.Ip,
                Port = _messageQueue.Port
            };

            try
            {
                var response = _messageHubConnection.SendMessageQueueSubscribeRequest(messageQueueSubscribeRequest, remoteEndpointInfo);
                ThrowResponseExceptionIfRequired(response);

                return response.SubscribeId;
            }
            catch (MessageConnectionException messageConnectionException)
            {
                throw new MessageQueueException("Error subscribing to message queue", messageConnectionException);
            }
        }

        public Task UnsubscribeAsync(string subscribeId)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Checks connection message response and throws an exception if an error
        /// </summary>
        /// <param name="message"></param>
        /// <exception cref="MessageConnectionException"></exception>
        private void ThrowResponseExceptionIfRequired(MessageBase message)
        {
            if (message.Response != null && message.Response.ErrorCode != null)
            {
                throw new MessageConnectionException(message.Response.ErrorMessage);
            }
        }
    }
}
