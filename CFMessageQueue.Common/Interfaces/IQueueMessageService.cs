using CFMessageQueue.Models;

namespace CFMessageQueue.Interfaces
{
    public interface IQueueMessageService : IEntityWithIdService<QueueMessage, string>
    {
        Task<List<QueueMessage>> GetExpired(string messageQueueId, DateTimeOffset now);
    }
}
