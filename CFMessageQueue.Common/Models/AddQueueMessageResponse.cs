using CFMessageQueue.Constants;

namespace CFMessageQueue.Models
{
    public class AddQueueMessageResponse :MessageBase
    {
        public string Ip { get; set; } = String.Empty;

        public int Port { get; set; }

        public AddQueueMessageResponse()
        {
            Id = Guid.NewGuid().ToString();
            TypeId = MessageTypeIds.AddQueueMessageResponse;
        }
    }
}
