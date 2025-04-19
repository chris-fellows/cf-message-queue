using CFConnectionMessaging.Models;
using CFMessageQueue.Constants;
using CFMessageQueue.Exceptions;
using CFMessageQueue.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace CFMessageQueue.Services
{
    public abstract class ClientConnectorBase        
    {
        /// <summary>
        /// Session for receiving messages
        /// </summary>
        protected class MessageReceiveSession
        {
            public string MessageId { get; internal set; }

            public Channel<Tuple<MessageBase, MessageReceivedInfo>> MessagesChannel = Channel.CreateBounded<Tuple<MessageBase, MessageReceivedInfo>>(50);


            public CancellationTokenSource CancellationTokenSource { get; internal set; }

            public MessageReceiveSession(string messageId, CancellationTokenSource cancellationTokenSource)
            {
                MessageId = messageId;
                CancellationTokenSource = cancellationTokenSource;
            }
        }

        /// <summary>
        /// Active response sessions. Sending a message and waiting for response
        /// </summary>
        protected Dictionary<string, MessageReceiveSession> _responseSessions = new();

        protected TimeSpan _responseTimeout = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Checks connection message response and throws an exception if an error
        /// </summary>
        /// <param name="message"></param>
        /// <exception cref="MessageConnectionException"></exception>
        protected void ThrowResponseExceptionIfRequired(MessageBase? message)
        {
            if (message == null)
            {
                throw new MessageConnectionException("Timeout receiving response");
            }

            if (message.Response != null && message.Response.ErrorCode != null)
            {
                throw new MessageConnectionException(message.Response.ErrorMessage);
            }
        }

        /// <summary>
        /// Waits for all response messages or timeout
        /// </summary>
        /// <param name="request"></param>
        /// <param name="messageReceiveSession"></param>
        /// <param name="timeout"></param>
        /// <returns>All responses received or empty if last response not received</returns>
        protected static async Task<List<MessageBase>> WaitForResponsesAsync(MessageBase request, MessageReceiveSession messageReceiveSession)
        {
            var cancellationToken = messageReceiveSession.CancellationTokenSource.Token;

            var reader = messageReceiveSession.MessagesChannel.Reader;

            var isGotAllResponses = false;
            var responseMessages = new List<MessageBase>();
            while (!isGotAllResponses &&
                !cancellationToken.IsCancellationRequested)
            {
                // Wait for message received or timeout
                var response = await reader.ReadAsync(cancellationToken);

                if (response != null && response.Item1 != null)
                {
                    var responseMessage = response.Item1;
                    if (responseMessage.Response.MessageId == request.Id)   // Sanity check
                    {
                        responseMessages.Add(responseMessage);

                        // Check if last response
                        isGotAllResponses = !responseMessage.Response.IsMore;
                    }
                }
            }

            return isGotAllResponses ? responseMessages : new();
        }
    }
}
