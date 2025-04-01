using CFMessageQueue.Models;

namespace CFMessageQueue.Interfaces
{
    public interface IMessageService : IEntityWithIdService<QueueMessage, string>
    {
    }
}
