using CFMessageQueue.Constants;
using CFMessageQueue.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CFMessageQueue.Models
{
    public class ConfigureMessageHubClientRequest : MessageBase
    {
        /// <summary>
        /// Message hub client being configured
        /// </summary>
        public string MessageHubClientId { get; set; } = String.Empty;

        public string MessageQueueId { get; set; } = String.Empty;

        public List<RoleTypes> RoleTypes { get; set; } = new();

        public ConfigureMessageHubClientRequest()
        {
            Id = Guid.NewGuid().ToString();
            TypeId = MessageTypeIds.ConfigureMessageHubClientRequest;                
        }
    }
}
