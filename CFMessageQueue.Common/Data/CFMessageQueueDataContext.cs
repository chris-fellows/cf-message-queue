using CFMessageQueue.Models;
using Microsoft.EntityFrameworkCore;

namespace CFMessageQueue.Data
{
    public class CFMessageQueueDataContext : DbContext
    {
        public CFMessageQueueDataContext(DbContextOptions<CFMessageQueueDataContext> options)
            : base(options)
        {
        }

        public DbSet<MessageHubClient> MessageHubClient { get; set; } = default!;

        public DbSet<QueueMessageInternal> QueueMessageInternal { get; set; } = default!;

        public DbSet<MessageQueue> MessageQueue { get; set; } = default;

        public DbSet<QueueMessageHub> QueueMessageHub { get; set; } = default;
    }
}
