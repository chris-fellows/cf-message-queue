using CFMessageQueue.Models;

namespace CFMessageQueue.Interfaces
{
    /// <summary>
    /// Message queue directory for providing details about message hubs and queues
    /// </summary>
    public interface IMessageQueueDirectory
    {
        /// <summary>
        /// Gets all message hubs
        /// </summary>
        /// <returns></returns>
        Task<List<QueueMessageHub>> GetMessageHubsAsync();

        /// <summary>
        /// Gets all message queues on message hub
        /// </summary>
        /// <returns></returns>
        Task<List<MessageQueue>> GetMessageQueuesAsync(QueueMessageHub messageHub);
    }
}
