using CFMessageQueue.Constants;

namespace CFMessageQueue.Models
{
    public class AddQueueMessageRequest : MessageBase
    {
        public QueueMessage QueueMessage { get; set; }

        public MessageQueue MessageQueue { get; set; }

        public AddQueueMessageRequest()
        {
            Id = Guid.NewGuid().ToString();
            TypeId = MessageTypeIds.AddQueueMessageRequest;
        }
    }
}
