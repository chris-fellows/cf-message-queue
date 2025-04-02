using CFMessageQueue.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CFMessageQueue.Models
{
    public class AddMessageHubClientResponse : MessageBase
    {
        public string MessageHubClientId { get; set; } = String.Empty;

        public AddMessageHubClientResponse()
        {
            Id = Guid.NewGuid().ToString();
            TypeId = MessageTypeIds.AddMessageHubClientResponse;
        }
    }
}
