using CFMessageQueue.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CFMessageQueue.Models
{
    /// <summary>
    /// Queue message (Internal format with serialized body)
    /// </summary>
    public class QueueMessageInternal
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
        /// Status
        /// </summary>
        public QueueMessageStatuses Status { get; set; }

        /// <summary>
        /// Processing message hub client
        /// </summary>
        public string ProcessingMessageHubClientId { get; set; } = String.Empty;

        public int MaxProcessingMilliseconds { get; set; }

        public DateTimeOffset ProcessingStartDateTime { get; set; }

        /// <summary>
        /// Expiry time (Seconds)
        /// </summary>
        public long ExpirySeconds { get; set; }

        /// <summary>
        /// Message content
        /// </summary>
        public byte[] Content { get; set; } = new byte[0];

        /// <summary>
        /// Content type (Type name)
        /// </summary>
        public string ContentType { get; set; } = String.Empty;
    }
}
