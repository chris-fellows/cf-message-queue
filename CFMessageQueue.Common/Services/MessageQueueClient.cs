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
    public class MessageQueueClient : IMessageQueueClient
    {
        private readonly MessageHubConnection _messageHubConnection;

        public async Task SendAsync(QueueMessage queueMessage, MessageQueue messageQueue)
        {
            var addQueueMessageRequest = new AddQueueMessageRequest()
            {
                QueueMessage = queueMessage,
                MessageQueue = messageQueue
            };

            var remoteEndpointInfo = new EndpointInfo()
            {
                Ip = messageQueue.IP,
                Port = messageQueue.Port
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

        public async Task<QueueMessage?> GetNextAsync(MessageQueue messageQueue)
        {
            var getNextQueueMessageRequest = new GetNextQueueMessageRequest()
            {
                MessageQueue = messageQueue
            };

            var remoteEndpointInfo = new EndpointInfo()
            {
                Ip = messageQueue.IP,
                Port = messageQueue.Port
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

        public async Task<string?> SubscribeAsync(MessageQueue messageQueue)
        {
            var messageQueueSubscribeRequest = new MessageQueueSubscribeRequest()
            {
                MessageQueue = messageQueue
            };

            var remoteEndpointInfo = new EndpointInfo()
            {
                Ip = messageQueue.IP,
                Port = messageQueue.Port
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
