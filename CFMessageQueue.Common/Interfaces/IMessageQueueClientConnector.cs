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
        /// Gets next message from queue
        /// </summary>
        /// <param name="messageQueue"></param>
        /// <returns></returns>
        Task<QueueMessage?> GetNextAsync();

        /// <summary>
        /// Subscribe for notifications from queue. E.g. Message(s) added
        /// </summary>
        /// <param name="notificationAction">Action to take< (Parameter=Event name)</param>        
        /// <param name="queueSizeNotificationFrequency">Frequency to notify of queue size (Zero=Never)</param>
        /// <returns></returns>
        Task<string> SubscribeAsync(Action<string> notificationAction, TimeSpan queueSizeNotificationFrequency);

        /// <summary>
        /// Unsubscribe from notifications from queue
        /// </summary>
        /// <param name="subscribeId"></param>
        /// <returns></returns>
        Task UnsubscribeAsync();
    }
}
