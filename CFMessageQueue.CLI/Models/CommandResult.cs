using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CFMessageQueue.CLI.Models
{
    internal class CommandResult
    {
        public List<string> Output = new List<string>();

        public bool Exit { get; set; }
    }
}
