using CFMessageQueue.Models;

namespace CFMessageQueue.Interfaces
{
    /// <summary>
    /// Message queue client. Performs queue level functions.
    /// </summary>
    public interface IMessageQueueClientConnector
    {        
        /// <summary>
        /// Current message queue. Must be set before interacting with message queue.
        /// </summary>
        MessageQueue? MessageQueue { get; set; }

        /// <summary>
        /// Sends message to message queue
        /// </summary>
        /// <param name="message"></param>
        /// <param name="messageQueue"></param>
        /// <returns></returns>
        Task SendAsync(QueueMessage message);

        /// <summary>
        /// Gets next message from queue. If no message then can wait until the max wait if set.
        /// 
        /// Caller should subsequently call SetProcessed to indicate if the message was processed or not.
        /// </summary>
        /// <param name="maxWait">Max time to wait for message: 0=Don't wait</param>
        /// <returns></returns>
        Task<QueueMessage?> GetNextAsync(TimeSpan maxWait);

        /// <summary>
        /// Sets message as processed or not. Expected to have previously called GetNextAsync.
        /// </summary>
        /// <param name="queueMessageId"></param>
        /// <param name="processed"></param>
        /// <returns></returns>
        Task SetProcessed(string queueMessageId, bool processed);

        /// <summary>
        /// Gets page of queue messages
        /// </summary>
        /// <param name="pageItems"></param>
        /// <param name="page"></param>
        /// <returns></returns>
        Task<List<QueueMessage>> GetQueueMessages(int pageItems, int page);

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
