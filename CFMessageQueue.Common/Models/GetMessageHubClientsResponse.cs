using CFMessageQueue.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CFMessageQueue.Models
{
    public class GetMessageHubClientsResponse : MessageBase
    {
        public List<MessageHubClient> MessageHubClients { get; set; } = new();

        public GetMessageHubClientsResponse()
        {
            Id = Guid.NewGuid().ToString();
            TypeId = MessageTypeIds.GetMessageHubClientsResponse;
        }
    }
}
