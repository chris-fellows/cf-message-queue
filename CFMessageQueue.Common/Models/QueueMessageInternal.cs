using CFMessageQueue.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
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
        [MaxLength(50)]
        public string Id { get; set; } = String.Empty;

        /// <summary>
        /// Message type
        /// </summary>
        [MaxLength(50)]
        public string TypeId { get; set; } = String.Empty;

        /// <summary>
        /// Sender message hub client. Does not need to be set by client, will be set by hub.
        /// </summary>
        [MaxLength(50)]
        [ForeignKey("SenderMessageHubClient")]
        public string SenderMessageHubClientId { get; set; } = String.Empty;

        public MessageHubClient SenderMessageHubClient { get; set; }

        /// <summary>
        /// Message queue. Does not need to be set by client, will be set by hub.
        /// </summary>       
        [MaxLength(50)]
        [ForeignKey("MessageQueue")]
        public string MessageQueueId { get; set; } = String.Empty;
        
        public MessageQueue MessageQueue { get; set; }

        /// <summary>
        /// Message name (User friendly description)
        /// </summary>
        [MaxLength(100)]
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
        /// Processing message hub client (if any)
        /// </summary>
        [MaxLength(50)]       
        [ForeignKey("ProcessingMessageHubClient")]
        public string? ProcessingMessageHubClientId { get; set; }

        public MessageHubClient? ProcessingMessageHubClient { get; set; }

        public int MaxProcessingMilliseconds { get; set; }

        public DateTimeOffset ProcessingStartDateTime { get; set; }

        /// <summary>
        /// Expiry time (Seconds)
        /// </summary>
        [Range(0, Int64.MaxValue)]
        public long ExpirySeconds { get; set; }

        /// <summary>
        /// Message content
        /// </summary>
        [MaxLength(Int32.MaxValue)]
        public byte[] Content { get; set; } = new byte[0];

        /// <summary>
        /// Content type (Type name)
        /// </summary>
        [MaxLength(100)]
        public string ContentType { get; set; } = String.Empty;
    }
}
