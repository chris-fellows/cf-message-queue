using CFMessageQueue.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CFMessageQueue.Models
{
    public class MessageQueueSubscribeRequest : MessageBase
    {
        public MessageQueue MessageQueue { get; set; }

        public MessageQueueSubscribeRequest()
        {
            Id = Guid.NewGuid().ToString();
            TypeId = MessageTypeIds.MessageQueueSubscribeRequest;
        }
    }
}
