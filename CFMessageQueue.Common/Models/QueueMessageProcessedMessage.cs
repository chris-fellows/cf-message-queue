using CFMessageQueue.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CFMessageQueue.Models
{
    public class QueueMessageProcessedMessage : MessageBase
    {

        public string MessageQueueId { get; set; } = String.Empty;

        public string QueueMessageId { get; set; } = String.Empty;

        public bool Processed { get; set; }


        public QueueMessageProcessedMessage()
        {
            Id = Guid.NewGuid().ToString();
            TypeId = MessageTypeIds.QueueMessageProcessedMessage;
        }
    }
}
