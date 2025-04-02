using CFMessageQueue.Constants;
using CFMessageQueue.Models;

namespace CFMessageQueue.Models
{
    public class GetMessageHubsResponse : MessageBase
    {
        public List<QueueMessageHub> MessageHubs { get; set; } = new();

        public GetMessageHubsResponse()
        {
            Id = Guid.NewGuid().ToString();
            TypeId = MessageTypeIds.GetMessageHubsResponse;            
        }
    }
}
