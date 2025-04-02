using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CFMessageQueue.Models
{
    /// <summary>
    /// Message hub client
    /// </summary>
    public class MessageHubClient
    {
        public string Id { get; set; } = String.Empty;

        public string SecurityKey { get; set; } = String.Empty;
    }
}
