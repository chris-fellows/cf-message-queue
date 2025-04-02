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
        public string MessageQueueId { get; set; } = String.Empty;

        public MessageQueueSubscribeRequest()
        {
            Id = Guid.NewGuid().ToString();
            TypeId = MessageTypeIds.MessageQueueSubscribeRequest;
        }
    }
}
