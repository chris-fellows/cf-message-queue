using CFMessageQueue.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CFMessageQueue.Models
{
    public class AddMessageQueueRequest : MessageBase
    {
        public string MessageQueueName { get; set; } = String.Empty;

        public AddMessageQueueRequest()
        {
            Id = Guid.NewGuid().ToString();
            TypeId = MessageTypeIds.AddMessageQueueRequest;
        }
    }
}
