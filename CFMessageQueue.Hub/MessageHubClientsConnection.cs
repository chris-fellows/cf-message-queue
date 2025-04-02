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
    public class MessageHubClientsConnection
    {
        private readonly ConnectionTcp _connection = new ConnectionTcp();

        private MessageConverterList _messageConverterList = new();

        private readonly IServiceProvider _serviceProvider;

        private readonly ISimpleLog _log;
        
        public delegate void ConnectionMessageReceived(ConnectionMessage connectionMessage, MessageReceivedInfo messageReceivedInfo);
        public event ConnectionMessageReceived? OnConnectionMessageReceived;

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

        public void SendGetMessageHubsResponse(GetMessageHubsResponse getMessageHubsResponse, MessageReceivedInfo messageReceivedInfo)
        {
            _connection.SendMessage(_messageConverterList.GetMessageHubsResponseConverter.GetConnectionMessage(getMessageHubsResponse), messageReceivedInfo.RemoteEndpointInfo);
        }

        public void SendGetMessageQueuesResponse(GetMessageQueuesResponse getMessageQueuesResponse, MessageReceivedInfo messageReceivedInfo)
        {
            _connection.SendMessage(_messageConverterList.GetMessageQueuesResponseConverter.GetConnectionMessage(getMessageQueuesResponse), messageReceivedInfo.RemoteEndpointInfo);
        }

        public void SendAddMessageHubClientResponse(AddMessageHubClientResponse addMessageHubClientResponse, MessageReceivedInfo messageReceivedInfo)
        {
            _connection.SendMessage(_messageConverterList.AddMessageHubClientResponseConverter.GetConnectionMessage(addMessageHubClientResponse), messageReceivedInfo.RemoteEndpointInfo);
        }

        public void SendAddMessageQueueResponse(AddMessageQueueResponse addMessageQueueResponse, MessageReceivedInfo messageReceivedInfo)
        {
            _connection.SendMessage(_messageConverterList.AddMessageQueueResponseConverter.GetConnectionMessage(addMessageQueueResponse), messageReceivedInfo.RemoteEndpointInfo);
        }

        public void SendConfigureMessageHubClientResponse(ConfigureMessageHubClientResponse configureMessageHubClientResponse, MessageReceivedInfo messageReceivedInfo)
        {
            _connection.SendMessage(_messageConverterList.ConfigureMessageHubClientResponseConverter.GetConnectionMessage(configureMessageHubClientResponse), messageReceivedInfo.RemoteEndpointInfo);
        }
    }
}
