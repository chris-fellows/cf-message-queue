using CFConnectionMessaging.Models;
using CFMessageQueue.Enums;
using CFMessageQueue.Exceptions;
using CFMessageQueue.Interfaces;
using CFMessageQueue.Models;

namespace CFMessageQueue.Services
{
    /// <summary>
    /// Message hub client connector. Communicates with hub worker.
    /// </summary>
    public class MessageHubClientConnector : IMessageHubClientConnector, IDisposable
    {
        private readonly MessageHubConnection _messageHubConnection = new MessageHubConnection();

        private readonly string _clientSessionId = Guid.NewGuid().ToString();
        private readonly EndpointInfo _remoteEndpointInfo;
        private readonly string _securityKey;        

        public MessageHubClientConnector(EndpointInfo remoteEndpointInfo, string securityKey, int localPort)
        {            
            _remoteEndpointInfo = remoteEndpointInfo;
            _securityKey = securityKey;

            _messageHubConnection.StartListening(localPort);
        }

        public void Dispose()
        {
            _messageHubConnection.Dispose();
        }

        public string CreateRandomSecurityKey()
        {
            return Guid.NewGuid().ToString();
        }

        public Task ConfigureMessageHubClientAsync(string messageHubClientId, List<RoleTypes> roleTypes)
        {            
            if (String.IsNullOrEmpty(messageHubClientId))
            {
                throw new ArgumentNullException(nameof(messageHubClientId));
            }

            var configureMessageHubClientRequest = new ConfigureMessageHubClientRequest()
            {
                SecurityKey = _securityKey,
                ClientSessionId = _clientSessionId,
                MessageHubClientId = messageHubClientId,
                MessageQueueId = "",
                RoleTypes = roleTypes     // Will return error if they specify non-hub roles
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

        public Task ConfigureMessageHubClientAsync(string messageHubClientId, string messageQueueId, List<RoleTypes> roleTypes)
        {
            if (String.IsNullOrEmpty(messageHubClientId))
            {
                throw new ArgumentNullException(nameof(messageHubClientId));
            }
            if (String.IsNullOrEmpty(messageQueueId))
            {
                throw new ArgumentNullException(nameof(messageQueueId));
            }

            var configureMessageHubClientRequest = new ConfigureMessageHubClientRequest()
            {
                SecurityKey = _securityKey,
                ClientSessionId = _clientSessionId,
                MessageHubClientId = messageHubClientId,
                MessageQueueId = messageQueueId,
                RoleTypes = roleTypes       // Will return error if they specify non-queue roles
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

        public Task<string> AddMessageQueueAsync(string name, int maxConcurrentProcessing, int maxSize)
        {
            if (String.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name));
            }
            if (maxConcurrentProcessing < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxConcurrentProcessing), "Max Concurrent Processing must be zero or more");
            }
            if (maxSize < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxSize), "Max Size must be zero or more");
            }

            var addMessageQueueRequest = new AddMessageQueueRequest()
            {
                SecurityKey = _securityKey,
                ClientSessionId = _clientSessionId,
                MessageQueueName = name,
                MaxConcurrentProcessing = maxConcurrentProcessing,
                MaxSize = maxSize
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
            if (String.IsNullOrEmpty(messageQueueId))
            {
                throw new ArgumentNullException(nameof(messageQueueId));
            }

            var executeMessageQueueActionRequest = new ExecuteMessageQueueActionRequest()
            {
                SecurityKey = _securityKey,
                ClientSessionId = _clientSessionId,
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
            if (String.IsNullOrEmpty(messageQueueId))
            {
                throw new ArgumentNullException(nameof(messageQueueId));
            }

            var executeMessageQueueActionRequest = new ExecuteMessageQueueActionRequest()
            {
                SecurityKey = _securityKey,
                ClientSessionId = _clientSessionId,
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

        public Task<string> AddMessageHubClientAsync(string name, string securityKey)
        {
            if (String.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name));
            }
            if (String.IsNullOrEmpty(securityKey))
            {
                throw new ArgumentNullException(nameof(securityKey));
            }

            var addMessageHubClientRequest = new AddMessageHubClientRequest()
            {
                SecurityKey = _securityKey,
                ClientSessionId = _clientSessionId,
                Name = name,
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

        public Task<List<MessageHubClient>> GetMessageHubClientsAsync()
        {
            var getMessageHubClientsRequest = new GetMessageHubClientsRequest()
            {
                SecurityKey = _securityKey,
                ClientSessionId = _clientSessionId,
            };

            try
            {
                var response = _messageHubConnection.SendGetMessageHubClientsRequest(getMessageHubClientsRequest, _remoteEndpointInfo);
                ThrowResponseExceptionIfRequired(response);

                return Task.FromResult(response.MessageHubClients);
            }
            catch (MessageConnectionException messageConnectionException)
            {
                throw new MessageQueueException("Error getting message hub clients", messageConnectionException);
            }
        }

        public Task<List<QueueMessageHub>> GetMessageHubsAsync()
        {
            var getMessageHubsRequest = new GetMessageHubsRequest()
            {
                SecurityKey = _securityKey,
                ClientSessionId = _clientSessionId,
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
                SecurityKey = _securityKey,
                ClientSessionId = _clientSessionId,
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
