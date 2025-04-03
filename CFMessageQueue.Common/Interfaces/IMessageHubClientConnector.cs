using CFMessageQueue.Enums;
using CFMessageQueue.Models;

namespace CFMessageQueue.Interfaces
{
    /// <summary>
    /// Message hub client
    /// </summary>
    public interface IMessageHubClientConnector
    {
        /// <summary>
        /// Configures message hub client. Refers to either hub config or specific message queue depending on whether
        /// <paramref name="messageQueueId"/> is set.
        /// </summary>
        /// <param name="messageHubClientId"></param>
        /// <param name="messageQueueId">Message queue (If not set then sets hub level config)</param>
        /// <param name="roleTypes"></param>
        /// <returns></returns>
        Task ConfigureMessageHubClientAsync(string messageHubClientId, string messageQueueId, List<RoleTypes> roleTypes);

        /// <summary>
        /// Adds message queue
        /// </summary>
        /// <param name="name"></param>
        /// <returns>Message Queue Id</returns>
        Task<string> AddMessageQueueAsync(string name);

        Task DeleteMessageQueueAsync(string messageQueueId);

        Task ClearMessageQueueAsync(string messageQueueId);

        /// <summary>
        /// Adds message hub client
        /// </summary>
        /// <param name="securityKey"></param>
        /// <returns>Message Hub Client Id</returns>
        Task<string> AddMessageHubClientAsync(string securityKey);

        /// <summary>
        /// Gets all message hubs
        /// </summary>
        /// <returns></returns>
        Task<List<QueueMessageHub>> GetMessageHubsAsync();

        /// <summary>
        /// Gets all message queues on message hub
        /// </summary>
        /// <returns></returns>
        Task<List<MessageQueue>> GetMessageQueuesAsync();
    }
}
