using CFConnectionMessaging;
using CFConnectionMessaging.Models;
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
    public class MessageHubConnection
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
    
        private bool IsResponseMessage(ConnectionMessage connectionMessage)
        {
            //   var responseMessageTypeIds = new[]
            //{
            //        MessageTypeIds.GetEventItemsResponse,
            //        MessageTypeIds.GetFileObjectResponse,
            //        MessageTypeIds.GetMonitorAgentsResponse,
            //        MessageTypeIds.GetMonitorItemsResponse,
            //        MessageTypeIds.GetSystemValueTypesResponse
            //   };

            //   return responseMessageTypeIds.Contains(connectionMessage.TypeId);

            return false;
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
            //switch (connectionMessage.TypeId)
            //{
            //    case MessageTypeIds.GetEventItemsResponse:
            //        return _messageConverters.GetEventItemsResponseConverter.GetExternalMessage(connectionMessage);

            //    case MessageTypeIds.GetFileObjectResponse:
            //        return _messageConverters.GetFileObjectResponseConverter.GetExternalMessage(connectionMessage);

            //    case MessageTypeIds.GetMonitorAgentsResponse:
            //        return _messageConverters.GetMonitorAgentsResponseConverter.GetExternalMessage(connectionMessage);

            //    case MessageTypeIds.GetMonitorItemsResponse:
            //        return _messageConverters.GetMonitorItemsResponseConverter.GetExternalMessage(connectionMessage);

            //    case MessageTypeIds.GetSystemValueTypesResponse:
            //        return _messageConverters.GetSystemValueTypesResponseConverter.GetExternalMessage(connectionMessage);

            //    case MessageTypeIds.EntityUpdated:
            //        return _messageConverters.EntityUpdatedConverter.GetExternalMessage(connectionMessage);
            //}

            return null;
        }
    }
}
