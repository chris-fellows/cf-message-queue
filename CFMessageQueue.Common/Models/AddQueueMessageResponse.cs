using CFMessageQueue.Constants;

namespace CFMessageQueue.Models
{
    public class AddQueueMessageResponse : MessageBase
    {
        public AddQueueMessageResponse()
        {
            Id = Guid.NewGuid().ToString();
            TypeId = MessageTypeIds.AddQueueMessageResponse;
        }
    }
}
