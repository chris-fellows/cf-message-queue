using CFConnectionMessaging.Models;

namespace CFMessageQueue.TestClient.Models
{
    internal class ProducerConfig
    {        
        public string MessageQueueName { get; set; } = String.Empty;

        public string DefaultSecurityKey { get; set; } = String.Empty;

        public string AdminSecurityKey { get; set; } = String.Empty;
        
        public EndpointInfo HubEndpointInfo { get; set; } = new();

        public TimeSpan DelayBetweenSend { get; set; } = TimeSpan.FromSeconds(10);

        public int LocalPort { get; set; }
    }
}
