using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CFMessageQueue.Models
{
    /// <summary>
    /// Queue message hub details
    /// </summary>
    public class QueueMessageHub
    {
        public string Id { get; set; } = String.Empty;

        public string Ip { get; set; } = String.Empty;

        /// <summary>
        /// Port for communicating with hub.
        /// </summary>
        public int Port { get; set; }

        public List<SecurityItem> SecurityItems { get; set; } = new();
    }
}
