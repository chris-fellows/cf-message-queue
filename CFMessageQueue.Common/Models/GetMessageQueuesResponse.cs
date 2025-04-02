using CFMessageQueue.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CFMessageQueue.Models
{
    public class GetMessageQueuesResponse : MessageBase
    {
        public List<MessageQueue> MessageQueues { get; set; } = new();

        public GetMessageQueuesResponse()
        {
            Id = Guid.NewGuid().ToString();
            TypeId = MessageTypeIds.GetMessageQueuesResponse;
        }
    }
}
