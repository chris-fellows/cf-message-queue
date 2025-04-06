using CFConnectionMessaging.Models;
using CFConnectionMessaging;
using CFMessageQueue.Constants;
using CFMessageQueue.Interfaces;
using CFMessageQueue.Logs;
using CFMessageQueue.Models;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CFMessageQueue.Hub
{
    /// <summary>
    /// Connection for clients for hub. Handles communications not relevant to specific queues. E.g. Get queue list.
    /// </summary>
    public class MessageHubClientsConnection : IDisposable
    {
        private readonly ConnectionTcp _connection = new ConnectionTcp();

        private MessageConverterList _messageConverterList = new();

        private readonly IServiceProvider _serviceProvider;

        private readonly ISimpleLog _log;
        
        public delegate void ConnectionMessageReceived(ConnectionMessage connectionMessage, MessageReceivedInfo messageReceivedInfo);
        public event ConnectionMessageReceived? OnConnectionMessageReceived;

        public delegate void ClientConnected(EndpointInfo endpointInfo);
        public event ClientConnected? OnClientConnected;

        public delegate void ClientDisconnected(EndpointInfo endpointInfo);
        public event ClientDisconnected? OnClientDisconnected;
           
        public MessageHubClientsConnection(IServiceProvider serviceProvider)
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

            _connection.OnClientConnected += delegate (EndpointInfo endpointInfo)
            {
                if (OnClientConnected != null)
                {
                    OnClientConnected(endpointInfo);
                }
            };

            _connection.OnClientDisconnected += delegate (EndpointInfo endpointInfo)
            {
                if (OnClientDisconnected != null)
                {
                    OnClientDisconnected(endpointInfo);
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

        public void SendGetMessageHubClientsResponse(GetMessageHubClientsResponse getMessageHubClientsResponse, EndpointInfo remoteEndpointInfo)
        {
            _connection.SendMessage(_messageConverterList.GetMessageHubClientsResponseConverter.GetConnectionMessage(getMessageHubClientsResponse), remoteEndpointInfo);
        }

        public void SendGetMessageHubsResponse(GetMessageHubsResponse getMessageHubsResponse, EndpointInfo remoteEndpointInfo)
        {
            _connection.SendMessage(_messageConverterList.GetMessageHubsResponseConverter.GetConnectionMessage(getMessageHubsResponse), remoteEndpointInfo);
        }

        public void SendGetMessageQueuesResponse(GetMessageQueuesResponse getMessageQueuesResponse, EndpointInfo remoteEndpointInfo)
        {
            _connection.SendMessage(_messageConverterList.GetMessageQueuesResponseConverter.GetConnectionMessage(getMessageQueuesResponse), remoteEndpointInfo);
        }

        public void SendAddMessageHubClientResponse(AddMessageHubClientResponse addMessageHubClientResponse, EndpointInfo remoteEndpointInfo)
        {
            _connection.SendMessage(_messageConverterList.AddMessageHubClientResponseConverter.GetConnectionMessage(addMessageHubClientResponse), remoteEndpointInfo);
        }

        public void SendAddMessageQueueResponse(AddMessageQueueResponse addMessageQueueResponse, EndpointInfo remoteEndpointInfo)
        {
            _connection.SendMessage(_messageConverterList.AddMessageQueueResponseConverter.GetConnectionMessage(addMessageQueueResponse), remoteEndpointInfo);
        }

        public void SendConfigureMessageHubClientResponse(ConfigureMessageHubClientResponse configureMessageHubClientResponse, EndpointInfo remoteEndpointInfo)
        {
            _connection.SendMessage(_messageConverterList.ConfigureMessageHubClientResponseConverter.GetConnectionMessage(configureMessageHubClientResponse), remoteEndpointInfo);
        }

        public void SendExecuteMessageQueueActionResponse(ExecuteMessageQueueActionResponse executeMessageQueueActionResponse, EndpointInfo remoteEndpointInfo)
        {
            _connection.SendMessage(_messageConverterList.ExecuteMessageQueueActionResponseConverter.GetConnectionMessage(executeMessageQueueActionResponse), remoteEndpointInfo);
        }
    }
}
