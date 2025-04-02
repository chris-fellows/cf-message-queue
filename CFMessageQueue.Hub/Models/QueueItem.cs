using CFConnectionMessaging.Models;
using CFMessageQueue.Hub.Enums;
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

        public ConnectionMessage? ConnectionMessage { get; set; }

        public MessageReceivedInfo? MessageReceivedInfo { get; set; }
    }
}
