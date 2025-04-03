using CFMessageQueue.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CFMessageQueue.Models
{
    public class ExecuteMessageQueueActionRequest : MessageBase
    {
        public string MessageQueueId { get; set; } = String.Empty;

        public string ActionName { get; set; } = String.Empty;

        public ExecuteMessageQueueActionRequest()
        {
            Id = Guid.NewGuid().ToString();
            TypeId = MessageTypeIds.ExecuteMessageQueueActionRequest;
        }
    }
}
