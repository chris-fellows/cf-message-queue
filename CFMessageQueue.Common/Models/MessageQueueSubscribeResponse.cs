using CFMessageQueue.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CFMessageQueue.Models
{
    public class MessageQueueSubscribeResponse : MessageBase
    {
        public string SubscribeId { get; set; } = String.Empty;

        public MessageQueueSubscribeResponse()
        {
            Id = Guid.NewGuid().ToString();
            TypeId = MessageTypeIds.MessageQueueSubscribeResponse;
        }
    }
}
