﻿namespace CFMessageQueue.Models
{
    /// <summary>
    /// Queue messaage (External format)
    /// </summary>    
    public class QueueMessage
    {
        public string Id { get; set; } = String.Empty;

        /// <summary>
        /// Message type
        /// </summary>
        public string TypeId { get; set; } = String.Empty;

        /// <summary>
        /// Sender message hub client. Does not need to be set by client, will be set by hub.
        /// </summary>
        public string SenderMessageHubClientId { get; set; } = String.Empty;

        /// <summary>
        /// Message queue. Does not need to be set by client, will be set by hub.
        /// </summary>
        public string MessageQueueId { get; set; } = String.Empty;

        /// <summary>
        /// Message name (User friendly description)
        /// </summary>
        public string Name { get; set; } = String.Empty;

        /// <summary>
        /// Created time
        /// </summary>
        public DateTimeOffset CreatedDateTime { get; set; }        

        /// <summary>
        /// Expiry time (Seconds)
        /// </summary>
        public long ExpirySeconds { get; set; }

        /// <summary>
        /// Message content
        /// </summary>
        public object? Content { get; set; }       
    }
}
