using CFMessageQueue.Models;

namespace CFMessageQueue.Interfaces
{
    /// <summary>
    /// Client interface to message queue
    /// </summary>
    public interface IMessageQueueClientConnector
    {
        void SetMessageQueue(MessageQueue messageQueue);

        /// <summary>
        /// Sends message to message queue
        /// </summary>
        /// <param name="message"></param>
        /// <param name="messageQueue"></param>
        /// <returns></returns>
        Task SendAsync(QueueMessage message);

        /// <summary>
        /// Gets next message from queue. If no message then can wait until the max wait if set
        /// </summary>
        /// <param name="maxWait">Max time to wait for message: 0=Don't wait</param>
        /// <returns></returns>
        Task<QueueMessage?> GetNextAsync(TimeSpan maxWait);

        /// <summary>
        /// Sets message as processed
        /// </summary>
        /// <param name="queueMessageId"></param>
        /// <param name="processed"></param>
        /// <returns></returns>
        Task SetProcessed(string queueMessageId, bool processed);

        /// <summary>
        /// Subscribe for notifications from queue. E.g. Message(s) added, queue size etc.        
        /// </summary>
        /// <param name="notificationAction">Action to take< (Parameters=Event name, Queue Size [nullable])</param>        
        /// <param name="queueSizeNotificationFrequency">Frequency to notify of queue size (Zero=Never)</param>
        /// <returns></returns>
        Task<string> SubscribeAsync(Action<string, long?> notificationAction, TimeSpan queueSizeNotificationFrequency);

        /// <summary>
        /// Unsubscribe from notifications from queue
        /// </summary>
        /// <param name="subscribeId"></param>
        /// <returns></returns>
        Task UnsubscribeAsync();
    }
}
