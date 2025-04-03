using CFConnectionMessaging.Models;
using CFMessageQueue.Enums;
using CFMessageQueue.Exceptions;
using CFMessageQueue.Interfaces;
using CFMessageQueue.Models;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CFMessageQueue.Services
{
    public class MessageHubClientConnector : IMessageHubClientConnector, IDisposable
    {
        private readonly MessageHubConnection _messageHubConnection = new MessageHubConnection();

        private readonly EndpointInfo _remoteEndpointInfo;
        private readonly string _adminSecurityKey;
        private readonly string _defaultSecurityKey;

        public MessageHubClientConnector(EndpointInfo remoteEndpointInfo, string adminSecurityKey, string defaultSecurityKey, int localPort)
        {            
            _remoteEndpointInfo = remoteEndpointInfo;
            _adminSecurityKey = adminSecurityKey;
            _defaultSecurityKey = defaultSecurityKey;

            _messageHubConnection.StartListening(localPort);
        }

        public void Dispose()
        {
            _messageHubConnection.Dispose();
        }

        public Task ConfigureMessageHubClientAsync(string messageHubClientId, string messageQueueId, List<RoleTypes> roleTypes)
        {
            var configureMessageHubClientRequest = new ConfigureMessageHubClientRequest()
            {
                SecurityKey = _adminSecurityKey,
                MessageHubClientId = messageHubClientId,
                MessageQueueId = messageQueueId,
                RoleTypes = roleTypes
            };

            try
            {
                var response = _messageHubConnection.SendConfigureMessageHubClientRequest(configureMessageHubClientRequest, _remoteEndpointInfo);
                ThrowResponseExceptionIfRequired(response);                
            }
            catch (MessageConnectionException messageConnectionException)
            {
                throw new MessageQueueException("Error configuring message hub client", messageConnectionException);
            }

            return Task.CompletedTask;
        }

        public Task<string> AddMessageQueueAsync(string name)
        {
            var addMessageQueueRequest = new AddMessageQueueRequest()
            {
                SecurityKey = _adminSecurityKey,
                MessageQueueName = name
            };

            try
            {
                var response = _messageHubConnection.SendAddMessageQueueRequest(addMessageQueueRequest, _remoteEndpointInfo);
                ThrowResponseExceptionIfRequired(response);

                return Task.FromResult(response.MessageQueueId);
            }
            catch (MessageConnectionException messageConnectionException)
            {
                throw new MessageQueueException("Error adding message queue", messageConnectionException);
            }
        }

        public Task DeleteMessageQueueAsync(string messageQueueId)
        {
            var executeMessageQueueActionRequest = new ExecuteMessageQueueActionRequest()
            {
                SecurityKey = _adminSecurityKey,
                MessageQueueId = messageQueueId,
                ActionName = "DELETE"
            };

            try
            {
                var response = _messageHubConnection.SendExecuteMessageQueueActionRequest(executeMessageQueueActionRequest, _remoteEndpointInfo);
                ThrowResponseExceptionIfRequired(response);                
            }
            catch (MessageConnectionException messageConnectionException)
            {
                throw new MessageQueueException("Error deleting message queue", messageConnectionException);
            }

            return Task.CompletedTask;
        }

        public Task ClearMessageQueueAsync(string messageQueueId)
        {
            var executeMessageQueueActionRequest = new ExecuteMessageQueueActionRequest()
            {
                SecurityKey = _adminSecurityKey,
                MessageQueueId = messageQueueId,
                ActionName = "CLEAR"
            };

            try
            {
                var response = _messageHubConnection.SendExecuteMessageQueueActionRequest(executeMessageQueueActionRequest, _remoteEndpointInfo);
                ThrowResponseExceptionIfRequired(response);                
            }
            catch (MessageConnectionException messageConnectionException)
            {
                throw new MessageQueueException("Error deleting message queue", messageConnectionException);
            }

            return Task.CompletedTask;
        }

        public Task<string> AddMessageHubClientAsync(string securityKey)
        {
            var addMessageHubClientRequest = new AddMessageHubClientRequest()
            {
                SecurityKey = _adminSecurityKey,
                ClientSecurityKey = securityKey
            };

            try
            {
                var response = _messageHubConnection.SendAddMessageHubClientRequest(addMessageHubClientRequest, _remoteEndpointInfo);
                ThrowResponseExceptionIfRequired(response);

                return Task.FromResult(response.MessageHubClientId);
            }
            catch (MessageConnectionException messageConnectionException)
            {
                throw new MessageQueueException("Error getting message hubs", messageConnectionException);
            }
        }

        public Task<List<QueueMessageHub>> GetMessageHubsAsync()
        {
            var getMessageHubsRequest = new GetMessageHubsRequest()
            {
                SecurityKey = _defaultSecurityKey
            };

            try
            {
                var response = _messageHubConnection.SendGetMessageHubsRequest(getMessageHubsRequest, _remoteEndpointInfo);
                ThrowResponseExceptionIfRequired(response);

                return Task.FromResult(response.MessageHubs);
            }
            catch (MessageConnectionException messageConnectionException)
            {
                throw new MessageQueueException("Error getting message hubs", messageConnectionException);
            }
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

        public Task<List<MessageQueue>> GetMessageQueuesAsync()
        {
            var getMessageQueuesRequest = new GetMessageQueuesRequest()
            {
                SecurityKey = _defaultSecurityKey
            };

            try
            {
                var response = _messageHubConnection.SendGetMessageQueuesRequest(getMessageQueuesRequest, _remoteEndpointInfo);
                ThrowResponseExceptionIfRequired(response);

                return Task.FromResult(response.MessageQueues);
            }
            catch (MessageConnectionException messageConnectionException)
            {
                throw new MessageQueueException("Error getting message queues", messageConnectionException);
            }
        }
    }
}
