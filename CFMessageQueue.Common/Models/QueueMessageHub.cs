﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CFMessageQueue.Models
{
    public class QueueMessageHub
    {
        public string Id { get; set; } = String.Empty;

        public string IP { get; set; } = String.Empty;

        public int Port { get; set; }
    }
}
