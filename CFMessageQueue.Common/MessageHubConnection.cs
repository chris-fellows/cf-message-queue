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

        public delegate void MessageReceived(MessageBase message, MessageReceivedInfo messageReceivedInfo);
        public event MessageReceived? OnMessageReceived;

        public MessageHubConnection()
        {
            _connection.OnConnectionMessageReceived += delegate (ConnectionMessage connectionMessage, MessageReceivedInfo messageReceivedInfo)
            {
                var externalMessage = _messageConverterList.GetExternalMessage(connectionMessage);

                if (OnMessageReceived != null)
                {
                    OnMessageReceived(externalMessage, messageReceivedInfo);
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

        public void SendMessage(MessageBase externalMessage, EndpointInfo remoteEndpointInfo)
        {
            _connection.SendMessage(_messageConverterList.GetConnectionMessage(externalMessage), remoteEndpointInfo);
        }     
    }
}
