using CFMessageQueue.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CFMessageQueue.Models
{
    public class GetQueueMessagesRequest : MessageBase
    {
        public string MessageQueueId { get; set; } = String.Empty;

        public int PageItems { get; set; } = 50;

        public int Page { get; set; } = 1;

        public GetQueueMessagesRequest()
        {
            Id = Guid.NewGuid().ToString();
            TypeId = MessageTypeIds.GetQueueMessagesRequest;
        }
    }
}
