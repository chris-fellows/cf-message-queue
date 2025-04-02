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
        /// Gets next message from queue
        /// </summary>
        /// <param name="messageQueue"></param>
        /// <returns></returns>
        Task<QueueMessage?> GetNextAsync(MessageQueue messageQueue);

        /// <summary>
        /// Subscribe for notifications from queue. E.g. Message(s) added
        /// </summary>
        /// <param name="messageQueue"></param>
        /// <returns></returns>
        Task<string> SubscribeAsync(MessageQueue messageQueue);

        /// <summary>
        /// Unsubscribe from notifications from queue
        /// </summary>
        /// <param name="subscribeId"></param>
        /// <returns></returns>
        Task UnsubscribeAsync(string subscribeId);
    }
}
