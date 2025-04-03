using CFMessageQueue.Models;

namespace CFMessageQueue.Interfaces
{
    public interface IQueueMessageInternalService : IEntityWithIdService<QueueMessageInternal, string>
    {
        Task<List<QueueMessageInternal>> GetExpired(string messageQueueId, DateTimeOffset now);
    }
}
