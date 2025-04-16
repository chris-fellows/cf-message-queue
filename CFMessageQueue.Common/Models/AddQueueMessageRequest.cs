using CFMessageQueue.Constants;

namespace CFMessageQueue.Models
{
    public class AddQueueMessageRequest : MessageBase
    {        
        public NewQueueMessageInternal QueueMessage { get; set; }

        public string MessageQueueId { get; set; } = String.Empty;

        public AddQueueMessageRequest()
        {
            Id = Guid.NewGuid().ToString();
            TypeId = MessageTypeIds.AddQueueMessageRequest;
        }
    }
}
