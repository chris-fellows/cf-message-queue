﻿using CFMessageQueue.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CFMessageQueue.Interfaces
{
    public interface IMessageHubClientService : IEntityWithIdService<MessageHubClient, string>
    {
    }
}
