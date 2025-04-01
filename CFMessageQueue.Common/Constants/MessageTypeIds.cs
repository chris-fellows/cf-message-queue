using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CFMessageQueue.Constants
{
    public static class MessageTypeIds
    {
        public const string AddQueueMessage = "AddQueueMessage";
        public const string GetMessageHubsRequest = "GetMessageHubsRequest";
        public const string GetMessageHubsResponse = "GetMessageHubsResponse";
    }
}
