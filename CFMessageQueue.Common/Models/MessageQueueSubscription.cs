using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CFMessageQueue.Models
{
    public class MessageQueueSubscription
    {
        public string Id { get; set; } = String.Empty;

        public string MessageQueueId { get; set; } = String.Empty;
    }
}
