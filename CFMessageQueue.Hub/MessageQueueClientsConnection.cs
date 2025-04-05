using CFConnectionMessaging;
using CFConnectionMessaging.Models;
using CFMessageQueue.Constants;
using CFMessageQueue.Interfaces;
using CFMessageQueue.Logs;
using CFMessageQueue.Models;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace CFMessageQueue.Hub
{
    /// <summary>
    /// Connection for clients for single message queue. Clients connection via specific port (TCP)
    /// </summary>
    public class MessageQueueClientsConnection : IDisposable
    {
        private readonly ConnectionTcp _connection = new ConnectionTcp();

        private MessageConverterList _messageConverterList = new();

        private readonly IServiceProvider _serviceProvider;

        private readonly ISimpleLog _log;

        public delegate void ConnectionMessageReceived(ConnectionMessage connectionMessage, MessageReceivedInfo messageReceivedInfo);
        public event ConnectionMessageReceived? OnConnectionMessageReceived;

        public MessageQueueClientsConnection(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _log = _serviceProvider.GetRequiredService<ISimpleLog>();

            _connection.OnConnectionMessageReceived += delegate (ConnectionMessage connectionMessage, MessageReceivedInfo messageReceivedInfo)
            {
                if (IsResponseMessage(connectionMessage))     // Inside Send... method waiting for response
                {
                    // No current requests do this
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
                if (_connection.IsListening)
                {
                    _connection.StopListening();
                }
                _connection.Dispose();                
            }
        }

        public MessageConverterList MessageConverterList => _messageConverterList;

        public void StartListening(int port)
        {
            _log.Log(DateTimeOffset.UtcNow, "Information", $"Listening on port {port}");

            _connection.ReceivePort = port;
            _connection.StartListening();
        }

        public void StopListening()
        {
            _log.Log(DateTimeOffset.UtcNow, "Information", "Stopping listening");
            _connection.StopListening();
        }
      
        private bool IsResponseMessage(ConnectionMessage connectionMessage)
        {
            return false;
        }

        public void SendAddQueueMessageResponse(AddQueueMessageResponse response, EndpointInfo remoteEndpointInfo)
        {
            _connection.SendMessage(_messageConverterList.AddQueueMessageResponseConverter.GetConnectionMessage(response), remoteEndpointInfo);
        }

        public void SendGetNextQueueMessageResponse(GetNextQueueMessageResponse response, EndpointInfo remoteEndpointInfo)
        {
            _connection.SendMessage(_messageConverterList.GetNextQueueMessageResponseConverter.GetConnectionMessage(response), remoteEndpointInfo);
        }

        public void SendMessageQueueSubscribeResponse(MessageQueueSubscribeResponse response, EndpointInfo remoteEndpointInfo)
        {
            _connection.SendMessage(_messageConverterList.MessageQueueSubscribeResponseConverter.GetConnectionMessage(response), remoteEndpointInfo);
        }

        public void SendMessageQueueNotificationMessage(MessageQueueNotificationMessage message, EndpointInfo remoteEndpointInfo)
        {
            _connection.SendMessage(_messageConverterList.MessageQueueNotificationMessageConverter.GetConnectionMessage(message), remoteEndpointInfo);
        }
    }
}
