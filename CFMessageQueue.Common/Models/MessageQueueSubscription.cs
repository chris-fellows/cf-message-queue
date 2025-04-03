using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CFMessageQueue.Models
{
    public class MessageQueueSubscription
    {
        /// <summary>
        /// Unique Id
        /// </summary>
        public string Id { get; set; } = String.Empty;

        /// <summary>
        /// Message queue for subscription
        /// </summary>
        public string MessageQueueId { get; set; } = String.Empty;

        /// <summary>
        /// Message hub client that has subscribed
        /// </summary>
        public string MessageHubClientId { get; set; } = String.Empty;
    }
}
