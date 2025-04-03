using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CFMessageQueue.Constants
{
    public static class MessageTypeIds
    {        
        public const string AddMessageHubClientRequest = "AddMessageHubClientRequest";
        public const string AddMessageHubClientResponse = "AddMessageHubClientResponse";

        public const string AddMessageQueueRequest = "AddMessageQueueRequest";
        public const string AddMessageQueueResponse = "AddMessageQueueResponse";

        public const string AddQueueMessageRequest = "AddQueueMessageRequest";
        public const string AddQueueMessageResponse = "AddQueueMessageResponse";

        public const string ConfigureMessageHubClientRequest = "ConfigureMessageHubClientRequest";
        public const string ConfigureMessageHubClientResponse = "ConfigureMessageHubClientResponse";

        public const string ExecuteMessageQueueActionRequest = "ExecuteMessageQueueActionRequest";
        public const string ExecuteMessageQueueActionResponse = "ExecuteMessageQueueActionResponse";

        public const string GetMessageHubsRequest = "GetMessageHubsRequest";
        public const string GetMessageHubsResponse = "GetMessageHubsResponse";

        public const string GetMessageQueuesRequest = "GetMessageQueuesRequest";
        public const string GetMessageQueuesResponse = "GetMessageQueuesResponse";

        public const string GetNextQueueMessageRequest = "GetNextQueueMessageRequest";
        public const string GetNextQueueMessageResponse = "GetNextQueueMessageResponse";

        public const string MessageQueueSubscribeRequest = "MessageQueueSubscribeRequest";
        public const string MessageQueueSubscribeResponse = "MessageQueueSubscribeResponse";
    }
}
