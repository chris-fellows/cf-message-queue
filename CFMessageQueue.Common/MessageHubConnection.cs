using CFConnectionMessaging;
using CFConnectionMessaging.Models;
using CFMessageQueue.Constants;
using CFMessageQueue.Exceptions;
using CFMessageQueue.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CFMessageQueue
{
    /// <summary>
    /// Connection to Message Hub
    /// </summary>
    public class MessageHubConnection : IDisposable
    {
        private ConnectionTcp _connection = new ConnectionTcp();

        private MessageConverterList _messageConverterList = new();

        public delegate void ConnectionMessageReceived(ConnectionMessage connectionMessage, MessageReceivedInfo messageReceivedInfo);
        public event ConnectionMessageReceived? OnConnectionMessageReceived;

        private TimeSpan _responseTimeout = TimeSpan.FromSeconds(30);

        private List<MessageBase> _responseMessages = new();

        public MessageHubConnection()
        {
            _connection.OnConnectionMessageReceived += delegate (ConnectionMessage connectionMessage, MessageReceivedInfo messageReceivedInfo)
            {
                if (IsResponseMessage(connectionMessage))     // Inside Send... method waiting for response
                {
                    _responseMessages.Add(GetExternalMessage(connectionMessage));                    
                }
                else
                {
                    if (OnConnectionMessageReceived != null)
                    {
                        OnConnectionMessageReceived(connectionMessage, messageReceivedInfo);
                    }
                }
            };
        }

        public void Dispose()
        {            
            if (_connection != null)
            {                
                _connection.Dispose();  // Stops listening, no need to call it explicitly
            }
        }

        public void StartListening(int port)
        {
            //_log.Log(DateTimeOffset.UtcNow, "Information", $"Listening on port {port}");

            _connection.ReceivePort = port;
            _connection.StartListening();
        }

        public void StopListening()
        {
            //_log.Log(DateTimeOffset.UtcNow, "Information", "Stopping listening");
            _connection.StopListening();
        }

        private bool IsResponseMessage(ConnectionMessage connectionMessage)
        {
            var responseMessageTypeIds = new[]
            {
                MessageTypeIds.AddMessageHubClientResponse,
                MessageTypeIds.AddMessageQueueResponse,
                MessageTypeIds.AddQueueMessageResponse,
                MessageTypeIds.ConfigureMessageHubClientResponse,
                MessageTypeIds.ExecuteMessageQueueActionResponse,
                MessageTypeIds.GetMessageHubsResponse,
                MessageTypeIds.GetMessageQueuesResponse,
                MessageTypeIds.GetNextQueueMessageResponse,
                MessageTypeIds.MessageQueueSubscribeResponse                 
            };

            return responseMessageTypeIds.Contains(connectionMessage.TypeId);            
        }

        public ExecuteMessageQueueActionResponse SendExecuteMessageQueueActionRequest(ExecuteMessageQueueActionRequest executeMessageQueueActionRequest, EndpointInfo remoteEndpointInfo)
        {
            _connection.SendMessage(_messageConverterList.ExecuteMessageQueueActionRequestConverter.GetConnectionMessage(executeMessageQueueActionRequest), remoteEndpointInfo);

            // Wait for response
            var responseMessages = new List<MessageBase>();
            var isGotAllMessages = WaitForResponses(executeMessageQueueActionRequest, _responseTimeout, _responseMessages,
                  (responseMessage) =>
                  {
                      responseMessages.Add(responseMessage);
                  });


            if (isGotAllMessages)
            {
                return (ExecuteMessageQueueActionResponse)responseMessages.First();
            }

            throw new MessageConnectionException("No response to execute message queue action");
        }


        public AddMessageHubClientResponse SendAddMessageHubClientRequest(AddMessageHubClientRequest addMessageHubClientRequest, EndpointInfo remoteEndpointInfo)
        {
            _connection.SendMessage(_messageConverterList.AddMessageHubClientRequestConverter.GetConnectionMessage(addMessageHubClientRequest), remoteEndpointInfo);

            // Wait for response
            var responseMessages = new List<MessageBase>();
            var isGotAllMessages = WaitForResponses(addMessageHubClientRequest, _responseTimeout, _responseMessages,
                  (responseMessage) =>
                  {
                      responseMessages.Add(responseMessage);
                  });


            if (isGotAllMessages)
            {
                return (AddMessageHubClientResponse)responseMessages.First();
            }

            throw new MessageConnectionException("No response to add message to queue");
        }

        public ConfigureMessageHubClientResponse SendConfigureMessageHubClientRequest(ConfigureMessageHubClientRequest configureMessageHubClientRequest, EndpointInfo remoteEndpointInfo)
        {
            _connection.SendMessage(_messageConverterList.ConfigureMessageHubClientRequestConverter.GetConnectionMessage(configureMessageHubClientRequest), remoteEndpointInfo);

            // Wait for response
            var responseMessages = new List<MessageBase>();
            var isGotAllMessages = WaitForResponses(configureMessageHubClientRequest, _responseTimeout, _responseMessages,
                  (responseMessage) =>
                  {
                      responseMessages.Add(responseMessage);
                  });


            if (isGotAllMessages)
            {
                return (ConfigureMessageHubClientResponse)responseMessages.First();
            }

            throw new MessageConnectionException("No response to configure message hub client");
        }

        public AddMessageQueueResponse SendAddMessageQueueRequest(AddMessageQueueRequest addMessageQueueRequest, EndpointInfo remoteEndpointInfo)
        {
            _connection.SendMessage(_messageConverterList.AddMessageQueueRequestConverter.GetConnectionMessage(addMessageQueueRequest), remoteEndpointInfo);

            // Wait for response
            var responseMessages = new List<MessageBase>();
            var isGotAllMessages = WaitForResponses(addMessageQueueRequest, _responseTimeout, _responseMessages,
                  (responseMessage) =>
                  {
                      responseMessages.Add(responseMessage);
                  });


            if (isGotAllMessages)
            {
                return (AddMessageQueueResponse)responseMessages.First();
            }

            throw new MessageConnectionException("No response to add message queue");
        }

        /// <summary>
        /// Sends AddQueueMessageMessage
        /// </summary>
        /// <param name="addQueueMessageMessage"></param>
        /// <param name="remoteEndpointInfo"></param>
        public AddQueueMessageResponse SendAddQueueMessageMessage(AddQueueMessageRequest addQueueMessageMessage, EndpointInfo remoteEndpointInfo)
        {
            _connection.SendMessage(_messageConverterList.AddQueueMessageRequestConverter.GetConnectionMessage(addQueueMessageMessage), remoteEndpointInfo);

            // Wait for response
            var responseMessages = new List<MessageBase>();
            var isGotAllMessages = WaitForResponses(addQueueMessageMessage, _responseTimeout, _responseMessages,
                  (responseMessage) =>
                  {
                      responseMessages.Add(responseMessage);
                  });


            if (isGotAllMessages)
            {
                return (AddQueueMessageResponse)responseMessages.First();
            }

            throw new MessageConnectionException("No response to add message to queue");
        }

        public GetMessageHubsResponse SendGetMessageHubsRequest(GetMessageHubsRequest getMessageHubsRequest, EndpointInfo remoteEndpointInfo)
        {
            _connection.SendMessage(_messageConverterList.GetMessageHubsRequestConverter.GetConnectionMessage(getMessageHubsRequest), remoteEndpointInfo);

            // Wait for response
            var responseMessages = new List<MessageBase>();
            var isGotAllMessages = WaitForResponses(getMessageHubsRequest, _responseTimeout, _responseMessages,
                  (responseMessage) =>
                  {
                      responseMessages.Add(responseMessage);
                  });

            if (isGotAllMessages)
            {
                return (GetMessageHubsResponse)responseMessages.First();
            }

            throw new MessageConnectionException("No response to get message hubs");
        }

        public GetMessageQueuesResponse SendGetMessageQueuesRequest(GetMessageQueuesRequest getMessageQueuesRequest, EndpointInfo remoteEndpointInfo)
        {
            _connection.SendMessage(_messageConverterList.GetMessageQueuesRequestConverter.GetConnectionMessage(getMessageQueuesRequest), remoteEndpointInfo);

            // Wait for response
            var responseMessages = new List<MessageBase>();
            var isGotAllMessages = WaitForResponses(getMessageQueuesRequest, _responseTimeout, _responseMessages,
                  (responseMessage) =>
                  {
                      responseMessages.Add(responseMessage);
                  });

            if (isGotAllMessages)
            {
                return (GetMessageQueuesResponse)responseMessages.First();
            }

            throw new MessageConnectionException("No response to get message queues");
        }

        public GetNextQueueMessageResponse SendGetNextQueueMessageRequest(GetNextQueueMessageRequest getNextQueueMessageRequest, EndpointInfo remoteEndpointInfo)
        {
            _connection.SendMessage(_messageConverterList.GetNextQueueMessageRequestConverter.GetConnectionMessage(getNextQueueMessageRequest), remoteEndpointInfo);

            // Wait for response
            var responseMessages = new List<MessageBase>();
            var isGotAllMessages = WaitForResponses(getNextQueueMessageRequest, _responseTimeout, _responseMessages,
                  (responseMessage) =>
                  {
                      responseMessages.Add(responseMessage);
                  });

            if (isGotAllMessages)
            {
                return (GetNextQueueMessageResponse)responseMessages.First();
            }

            throw new MessageConnectionException("No response to get next queue message");
        }

        public MessageQueueSubscribeResponse SendMessageQueueSubscribeRequest(MessageQueueSubscribeRequest messageQueueSubscribeRequest, EndpointInfo remoteEndpointInfo)
        {
            _connection.SendMessage(_messageConverterList.MessageQueueSubscribeRequestConverter.GetConnectionMessage(messageQueueSubscribeRequest), remoteEndpointInfo);

            // Wait for response
            var responseMessages = new List<MessageBase>();
            var isGotAllMessages = WaitForResponses(messageQueueSubscribeRequest, _responseTimeout, _responseMessages,
                  (responseMessage) =>
                  {
                      responseMessages.Add(responseMessage);
                  });

            if (isGotAllMessages)
            {
                return (MessageQueueSubscribeResponse)responseMessages.First();
            }

            throw new MessageConnectionException("No response to get next queue message");
        }

        /// <summary>
        /// Waits for all responses for request until completed or timeout. Where multiple responses are required then
        /// MessageBase.Response.IsMore=true for all except the last one.
        /// </summary>
        /// <param name="request">Request to check</param>
        /// <param name="timeout">Timeout receiving responses</param>
        /// <param name="responseMessagesToCheck">List where responses are added</param>
        /// <param name="responseMessageAction">Action to forward next response</param>
        /// <returns>Whether all responses received</returns>
        private static bool WaitForResponses(MessageBase request, TimeSpan timeout,
                                      List<MessageBase> responseMessagesToCheck,
                                      Action<MessageBase> responseMessageAction)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var isGotAllResponses = false;
            while (!isGotAllResponses &&
                    stopwatch.Elapsed < timeout)
            {
                // Check for next response message
                var responseMessage = responseMessagesToCheck.FirstOrDefault(m => m.Response != null && m.Response.MessageId == request.Id);

                if (responseMessage != null)
                {
                    // Discard
                    responseMessagesToCheck.Remove(responseMessage);

                    // Check if last response
                    isGotAllResponses = !responseMessage.Response.IsMore;

                    // Pass response to caller
                    responseMessageAction(responseMessage);
                }

                if (!isGotAllResponses) Thread.Sleep(20);
            }

            return isGotAllResponses;
        }

        public MessageBase? GetExternalMessage(ConnectionMessage connectionMessage)
        {
            switch(connectionMessage.TypeId)
            {
                case MessageTypeIds.AddMessageHubClientResponse:
                    return _messageConverterList.AddMessageHubClientResponseConverter.GetExternalMessage(connectionMessage);
                case MessageTypeIds.AddMessageQueueResponse:
                    return _messageConverterList.AddMessageQueueResponseConverter.GetExternalMessage(connectionMessage);
                case MessageTypeIds.AddQueueMessageResponse:
                    return _messageConverterList.AddQueueMessageResponseConverter.GetExternalMessage(connectionMessage);
                case MessageTypeIds.ConfigureMessageHubClientResponse:
                    return _messageConverterList.ConfigureMessageHubClientResponseConverter.GetExternalMessage(connectionMessage);
                case MessageTypeIds.GetMessageHubsResponse:
                    return _messageConverterList.GetMessageHubsResponseConverter.GetExternalMessage(connectionMessage);
                case MessageTypeIds.GetMessageQueuesResponse:
                    return _messageConverterList.GetMessageQueuesResponseConverter.GetExternalMessage(connectionMessage);
                case MessageTypeIds.GetNextQueueMessageResponse:
                    return _messageConverterList.GetNextQueueMessageResponseConverter.GetExternalMessage(connectionMessage);
                case MessageTypeIds.MessageQueueSubscribeResponse:
                    return _messageConverterList.MessageQueueSubscribeResponseConverter.GetExternalMessage(connectionMessage);
            }

            return null;
        }
    }
}
