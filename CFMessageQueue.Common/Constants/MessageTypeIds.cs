using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CFMessageQueue.Constants
{
    public static class MessageTypeIds
    {
        public const string AddQueueMessageRequest = "AddQueueMessageRequest";
        public const string AddQueueMessageResponse = "AddQueueMessageResponse";
        public const string GetMessageHubsRequest = "GetMessageHubsRequest";
        public const string GetMessageHubsResponse = "GetMessageHubsResponse";
        public const string GetNextQueueMessageRequest = "GetNextQueueMessageRequest";
        public const string GetNextQueueMessageResponse = "GetNextQueueMessageResponse";

        public const string MessageQueueSubscribeRequest = "MessageQueueSubscribeRequest";
        public const string MessageQueueSubscribeResponse = "MessageQueueSubscribeResponse";
    }
}
