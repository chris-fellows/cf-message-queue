using CFConnectionMessaging.Models;
using CFConnectionMessaging;
using CFMessageQueue.Logs;
using CFMessageQueue.Models;
using Microsoft.Extensions.DependencyInjection;

namespace CFMessageQueue.Hub
{
    /// <summary>
    /// Connection for clients for hub
    /// </summary>
    public class MessageHubClientsConnection : IDisposable
    {
        private readonly ConnectionTcp _connection = new ConnectionTcp();

        private MessageConverterList _messageConverterList = new();

        private readonly IServiceProvider _serviceProvider;

        private readonly ISimpleLog _log;
        
        public delegate void MessageReceived(MessageBase message, MessageReceivedInfo messageReceivedInfo);
        public event MessageReceived? OnMessageReceived;

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
                if (OnMessageReceived != null)
                {                    
                    OnMessageReceived(_messageConverterList.GetExternalMessage(connectionMessage), messageReceivedInfo);
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

        public void SendMessage(MessageBase externalMessage, EndpointInfo remoteEndpointInfo)
        {
            _connection.SendMessage(_messageConverterList.GetConnectionMessage(externalMessage), remoteEndpointInfo);
        }    
    }
}
