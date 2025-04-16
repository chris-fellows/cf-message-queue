using CFMessageQueue.Models;

namespace CFMessageQueue.Interfaces
{
    public interface IQueueMessageInternalService : IEntityWithIdService<QueueMessageInternal, string>
    {
        Task<List<QueueMessageInternal>> GetExpiredAsync(string messageQueueId, DateTimeOffset now);

        Task<List<QueueMessageInternal>> GetByMessageQueueAsync(string messageQueueId);

        Task<QueueMessageInternal> GetNextAsync(string messageQueueId);
    }
}
