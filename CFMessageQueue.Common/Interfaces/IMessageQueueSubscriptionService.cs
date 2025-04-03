using CFMessageQueue.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CFMessageQueue.Interfaces
{
    public interface IMessageQueueSubscriptionService : IEntityWithIdService<MessageQueueSubscription, string>
    {
    }
}
