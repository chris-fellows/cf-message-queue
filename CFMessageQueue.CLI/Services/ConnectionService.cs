using CFConnectionMessaging.Models;
using CFMessageQueue.CLI.Interfaces;
using CFMessageQueue.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CFMessageQueue.CLI.Services
{
    internal class ConnectionService : IConnectionService
    {
        public string SecurityKey { get; set; } = String.Empty;

        public EndpointInfo RemoteEndpointInfo { get; set; } = new();

        public IMessageHubClientConnector? MessageHubClientConnector { get; set; }

        public IMessageQueueClientConnector? MessageQueueClientConnector { get; set; }
    }
}
