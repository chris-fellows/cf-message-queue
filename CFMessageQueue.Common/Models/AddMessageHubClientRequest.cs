using CFMessageQueue.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CFMessageQueue.Models
{
    public class AddMessageHubClientRequest : MessageBase
    {
        public string Name { get; set; } = String.Empty;

        public string ClientSecurityKey { get; set; } = String.Empty;

        public AddMessageHubClientRequest()
        {
            Id = Guid.NewGuid().ToString();
            TypeId = MessageTypeIds.AddMessageHubClientRequest;
        }
    }
}
