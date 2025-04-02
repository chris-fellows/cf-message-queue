using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CFMessageQueue.Models
{
    public class MessageQueue
    {
        public string Id { get; set; } = String.Empty;

        public string Name { get; set; } = String.Empty;

        public string Ip { get; set; } = String.Empty;

        public int Port { get; set; }

        public List<SecurityItem> SecurityItems { get; set; } = new();
    }
}
