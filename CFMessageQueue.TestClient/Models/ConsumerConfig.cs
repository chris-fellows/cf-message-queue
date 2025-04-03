using CFConnectionMessaging.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CFMessageQueue.TestClient.Models
{
    internal class ConsumerConfig
    {
        public string MessageHubClientId { get; set; } = String.Empty;

        public string MessageQueueName { get; set; } = String.Empty;

        public string DefaultSecurityKey { get; set; } = String.Empty;

        public string AdminSecurityKey { get; set; } = String.Empty;

        public int LocalPort { get; set; }

        public EndpointInfo HubEndpointInfo { get; set; } = new();

        public TimeSpan DelayBetweenGetMessage { get; set; } = TimeSpan.FromSeconds(10);
    }
}
