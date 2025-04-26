using CFMessageQueue.Hub.Models;
using CFMessageQueue.Models;

namespace CFMessageQueue.Hub
{
    internal class HubResources
    {
        public SystemConfig SystemConfig { get; set; }

        public QueueMessageHub QueueMessageHub { get; set; }
        public DateTimeOffset MessageHubClientsLastRefresh { get; set; } = DateTimeOffset.MinValue;
        public Dictionary<string, MessageHubClient> MessageHubClientsBySecurityKey { get; set; } = new Dictionary<string, MessageHubClient>();

        public List<MessageQueueWorker> MessageQueueWorkers { get; set; } = new List<MessageQueueWorker>();

        public MessageHubClientsConnection ClientsConnection { get; set; }

        public Mutex? QueueMutex = new Mutex();

        public MessageQueue? MessageQueue { get; set; }

        public List<ClientQueueSubscription> ClientQueueSubscriptions { get; set; } = new List<ClientQueueSubscription>();

        public List<QueueMessageInternal> QueueMessageInternalProcessing { get; set; } = new List<QueueMessageInternal>();
        
    }
}
