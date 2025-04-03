using CFMessageQueue.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CFMessageQueue.Models
{
    /// <summary>
    /// Message queue notification message. E.g. Message added
    /// </summary>
    public class MessageQueueNotificationMessage : MessageBase
    {
        public string EventName { get; set; } = String.Empty;

        public long QueueSize { get; set; }

        public MessageQueueNotificationMessage()
        {
            Id = Guid.NewGuid().ToString();
            TypeId = MessageTypeIds.MessageQueueNotification;
        }
    }
}
