using CFMessageQueue.Constants;

namespace CFMessageQueue.Models
{
    public class QueueMessageProcessedMessage : MessageBase
    {
        /// <summary>
        /// Message queue
        /// </summary>
        public string MessageQueueId { get; set; } = String.Empty;

        /// <summary>
        /// Queue message
        /// </summary>
        public string QueueMessageId { get; set; } = String.Empty;

        /// <summary>
        /// Whether message was processed
        /// </summary>
        public bool Processed { get; set; }


        public QueueMessageProcessedMessage()
        {
            Id = Guid.NewGuid().ToString();
            TypeId = MessageTypeIds.QueueMessageProcessedMessage;
        }
    }
}
