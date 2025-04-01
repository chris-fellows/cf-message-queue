using CFMessageQueue.Common.Models;
using CFMessageQueue.Models;

namespace CFMessageQueue.Interfaces
{
    public interface IMessageQueueDirectory
    {
        /// <summary>
        /// Gets all message hubs
        /// </summary>
        /// <returns></returns>
        Task<List<MessageHub>> GetMessageHubsAsync();

        /// <summary>
        /// Gets all message queues on message hub
        /// </summary>
        /// <returns></returns>
        Task<List<MessageQueue>> GetMessageQueuesAsync(MessageHub messageHub);
    }
}
