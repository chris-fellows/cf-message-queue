using CFConnectionMessaging.Models;
using CFMessageQueue.Hub.Enums;
using CFMessageQueue.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CFMessageQueue.Hub.Models
{
    public class QueueItem
    {
        public QueueItemTypes ItemType { get; set; }

        public MessageBase? Message { get; set; }

        public MessageReceivedInfo? MessageReceivedInfo { get; set; }
    }
}
