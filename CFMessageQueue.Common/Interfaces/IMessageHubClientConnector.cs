using CFMessageQueue.Enums;
using CFMessageQueue.Models;

namespace CFMessageQueue.Interfaces
{
    /// <summary>
    /// Message hub client. Performs hub level functions.
    /// </summary>
    public interface IMessageHubClientConnector
    {
        /// <summary>
        /// Creates random security key, typically for new client
        /// </summary>
        /// <returns></returns>
        string CreateRandomSecurityKey();

        /// <summary>
        /// Configure message hub client permissions for hub level functions
        /// 
        /// To remove permissions then pass empty role types list
        /// </summary>
        /// <param name="messageHubClientId"></param>
        /// <param name="roleTypes"></param>
        /// <returns></returns>
        Task ConfigureMessageHubClientAsync(string messageHubClientId, List<RoleTypes> roleTypes);

        /// <summary>
        /// Configure message hub client permissions for queue level functions
        /// 
        /// To remove permissions then pass empty role types list
        /// <paramref name="messageQueueId"/> is set.
        /// </summary>
        /// <param name="messageHubClientId"></param>
        /// <param name="messageQueueId">Message queue (If not set then sets hub level config)</param>
        /// <param name="roleTypes"></param>
        /// <returns></returns>
        Task ConfigureMessageHubClientAsync(string messageHubClientId, string messageQueueId, List<RoleTypes> roleTypes);

        /// <summary>
        /// Adds message queue
        /// 
        /// If messages must be processed in sequence then pass maxConcurrentProcessing=1
        /// </summary>
        /// <param name="name"></param>
        /// <param name="maxConcurrentProcessing">Max number of concurrent queue messages than can be processed</param>
        /// <param name="maxSize">Max queue size (0=Unlimited)</param>
        /// <returns>Message Queue Id</returns>
        Task<string> AddMessageQueueAsync(string name, int maxConcurrentProcessing, int maxSize);

        /// <summary>
        /// Deletes message queue
        /// </summary>
        /// <param name="messageQueueId"></param>
        /// <returns></returns>
        Task DeleteMessageQueueAsync(string messageQueueId);

        /// <summary>
        /// Clears message queue
        /// </summary>
        /// <param name="messageQueueId"></param>
        /// <returns></returns>
        Task ClearMessageQueueAsync(string messageQueueId);

        /// <summary>
        /// Adds message hub client
        /// </summary>
        /// <param name="securityKey">Security key</param>
        /// <returns>Message Hub Client Id</returns>
        Task<string> AddMessageHubClientAsync(string name, string securityKey);

        /// <summary>
        /// Gets all message hub clients
        /// </summary>
        /// <returns></returns>
        Task<List<MessageHubClient>> GetMessageHubClientsAsync();

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
