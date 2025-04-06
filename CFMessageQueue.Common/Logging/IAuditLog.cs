using CFMessageQueue.Models;

namespace CFMessageQueue.Logging
{
    /// <summary>
    /// Audit log for more structured data
    /// </summary>
    public interface IAuditLog
    {
        /// <summary>
        /// Log queue message item
        /// </summary>
        /// <param name="time"></param>
        /// <param name="action"></param>
        /// <param name="messageQueueName"></param>
        /// <param name="queueMessage"></param>
        void LogQueueMessage(DateTimeOffset date, string action, string messageQueueName, QueueMessageInternal queueMessage);

        /// <summary>
        /// Log queue item
        /// </summary>
        /// <param name="date"></param>
        /// <param name="action"></param>
        /// <param name="messageQueueName"></param>
        void LogQueue(DateTimeOffset date, string action, string messageQueueName);
    }
}
