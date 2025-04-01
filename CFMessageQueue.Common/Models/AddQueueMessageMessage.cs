using CFMessageQueue.Constants;

namespace CFMessageQueue.Models
{
    public class AddQueueMessageMessage : MessageBase
    {
        public QueueMessage QueueMessage { get; set; }

        public MessageQueue MessageQueue { get; set; }

        public AddQueueMessageMessage()
        {
            Id = Guid.NewGuid().ToString();
            TypeId = MessageTypeIds.AddQueueMessage;
        }
    }
}
