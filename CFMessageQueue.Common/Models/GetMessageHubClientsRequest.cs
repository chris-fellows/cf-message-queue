using CFMessageQueue.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CFMessageQueue.Models
{
    public class GetMessageHubClientsRequest : MessageBase
    {
        public GetMessageHubClientsRequest()
        {
            Id = Guid.NewGuid().ToString();
            TypeId = MessageTypeIds.GetMessageHubClientsRequest;
        }
    }
}
