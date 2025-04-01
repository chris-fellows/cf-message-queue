using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CFMessageQueue.Common.Models
{
    public class MessageHub
    {
        public string Id { get; set; } = String.Empty;

        public string IP { get; set; } = String.Empty;

        public int Port { get; set; }
    }
}
