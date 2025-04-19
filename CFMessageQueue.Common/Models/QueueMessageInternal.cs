using CFMessageQueue.Enums;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CFMessageQueue.Models
{
    /// <summary>
    /// Queue message (Internal format with serialized body)
    /// </summary>
    [Index(nameof(MessageQueueId), nameof(Priority), nameof(CreatedDateTime))]      // Used by IQueueMessageInternal.GetNextAsync
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
        /// Message priority for processing
        /// </summary>
        [Range(0, 100)]
        public short Priority { get; set; }

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
        public DateTime CreatedDateTime { get; set; }

        /// <summary>
        /// Expiry time
        /// </summary>
        public DateTime ExpiryDateTime { get; set; }

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

        /// <summary>
        /// Max time allowed for processing after which processing is cancelled and Status is reset
        /// </summary>
        [Range(0, Int32.MaxValue)]
        public int MaxProcessingSeconds { get; set; }

        /// <summary>
        /// Time processing started
        /// </summary>
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
        [MaxLength(200)]
        public string ContentType { get; set; } = String.Empty;
    }
}
