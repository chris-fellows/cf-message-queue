using CFMessageQueue.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CFMessageQueue.Models
{
    public class GetQueueMessagesResponse : MessageBase
    {
        public List<QueueMessageInternal> QueueMessages { get; set; } = new();

        public GetQueueMessagesResponse()
        {
            Id = Guid.NewGuid().ToString();
            TypeId = MessageTypeIds.GetQueueMessagesResponse;
        }
    }
}
