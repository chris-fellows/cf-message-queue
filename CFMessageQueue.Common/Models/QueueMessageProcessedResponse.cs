﻿using CFMessageQueue.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CFMessageQueue.Models
{
    public class QueueMessageProcessedResponse : MessageBase
    {
        public QueueMessageProcessedResponse()
        {
            Id = Guid.NewGuid().ToString();
            TypeId = MessageTypeIds.QueueMessageProcessedResponse;
        }
    }
}
