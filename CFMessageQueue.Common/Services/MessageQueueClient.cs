using CFMessageQueue.Interfaces;
using CFMessageQueue.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CFMessageQueue.Services
{
    public class MessageQueueClient : IMessageQueueClient
    {
        public async Task SendAsync(QueueMessage message, MessageQueue messageQueue)
        {

        }

        public void Subscribe(MessageQueue messageQueue)
        {
            string subscriptionId = Guid.NewGuid().ToString();

            return subscriptionId;
        }
    }
}
