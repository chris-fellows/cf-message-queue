using CFMessageQueue.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CFMessageQueue.Interfaces
{
    public interface IMessageQueueClient
    {
        /// <summary>
        /// Sends message to message queue
        /// </summary>
        /// <param name="message"></param>
        /// <param name="messageQueue"></param>
        /// <returns></returns>
        Task SendAsync(QueueMessage message, MessageQueue messageQueue);

        /// <summary>
        /// Subscribes to messages sent to queue
        /// </summary>
        /// <param name="messageQueue"></param>
        /// <returns></returns>
        string Subscribe(MessageQueue messageQueue);
    }
}
