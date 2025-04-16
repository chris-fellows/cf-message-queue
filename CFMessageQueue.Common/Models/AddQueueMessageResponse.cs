using CFMessageQueue.Constants;

namespace CFMessageQueue.Models
{
    public class AddQueueMessageResponse : MessageBase
    {
        public string QueueMessageId { get; set; } = String.Empty;

        public AddQueueMessageResponse()
        {
            Id = Guid.NewGuid().ToString();
            TypeId = MessageTypeIds.AddQueueMessageResponse;
        }
    }
}
