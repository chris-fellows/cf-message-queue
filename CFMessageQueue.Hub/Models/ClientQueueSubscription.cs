using CFConnectionMessaging.Models;

namespace CFMessageQueue.Hub.Models
{
    /// <summary>
    /// Client queue subscription.    
    /// </summary>
    internal class ClientQueueSubscription
    {
        /// <summary>
        /// Subscription Id
        /// </summary>
        public string Id { get; set; } = String.Empty;

        /// <summary>
        /// Client Session Id
        /// </summary>
        public string ClientSessionId { get; set; } = String.Empty;

        /// <summary>
        /// Message hub client
        /// </summary>
        public string MessageHubClientId { get; set; } = String.Empty;

        /// <summary>
        /// Client remote endpoint
        /// </summary>
        public EndpointInfo RemoteEndpointInfo { get; set; }

        /// <summary>
        /// Frequency to notify queue size (0=Never)
        /// </summary>
        public long QueueSizeFrequencySecs { get; set; }

        public DateTimeOffset LastNotifyQueueSize { get; set; } = DateTimeOffset.UtcNow;

        /// <summary>
        /// Notify client of queue size
        /// </summary>
        public bool DoNotifyQueueSize { get; set; }

        /// <summary>
        /// Whether to notify client if a new message is added.
        /// </summary>
        public bool NotifyIfMessageAdded { get; set; }

        /// <summary>
        /// Notify client that message was added
        /// </summary>
        public bool DoNotifyMessageAdded { get; set; }

        /// <summary>
        /// Notifyt client that queue was cleared
        /// </summary>
        public bool DoNotifyQueueCleared { get; set; }

        /// <summary>
        /// Whether any notification is required
        /// </summary>
        public bool IsNotificationRequired => DoNotifyQueueSize || DoNotifyMessageAdded || DoNotifyQueueCleared;
    }
}
