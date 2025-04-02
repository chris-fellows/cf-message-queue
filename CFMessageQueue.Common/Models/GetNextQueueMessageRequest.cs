using CFMessageQueue.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CFMessageQueue.Models
{
    public class GetNextQueueMessageRequest : MessageBase
    {
        public MessageQueue MessageQueue { get; set; }

        public GetNextQueueMessageRequest()
        {
            Id = Guid.NewGuid().ToString();
            TypeId = MessageTypeIds.GetNextQueueMessageRequest;
        }
    }
}
