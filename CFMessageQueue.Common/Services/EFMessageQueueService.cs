using CFMessageQueue.Data;
using CFMessageQueue.Interfaces;
using CFMessageQueue.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CFMessageQueue.Services
{
    public class EFMessageQueueService : EFBaseService, IMessageQueueService
    {
        public EFMessageQueueService(IDbContextFactory<CFMessageQueueDataContext> dbFactory) : base(dbFactory)
        {

        }

        public List<MessageQueue> GetAll()
        {
            return Context.MessageQueue
                .Include(m => m.SecurityItems)
                .ToList();
        }

        public async Task<List<MessageQueue>> GetAllAsync()
        {
            return await Context.MessageQueue
                .Include(m => m.SecurityItems)
                .ToListAsync();
        }

        public async Task<MessageQueue> AddAsync(MessageQueue messageHubClient)
        {
            Context.MessageQueue.Add(messageHubClient);
            await Context.SaveChangesAsync();
            return messageHubClient;
        }

        public async Task<MessageQueue> UpdateAsync(MessageQueue messageHubClient)
        {
            Context.MessageQueue.Update(messageHubClient);
            await Context.SaveChangesAsync();
            return messageHubClient;
        }

        public async Task<MessageQueue?> GetByIdAsync(string id)
        {
            var messageHubClient = await Context.MessageQueue
                .Include(m => m.SecurityItems)
                .FirstOrDefaultAsync(i => i.Id == id);
            return messageHubClient;
        }

        public async Task<List<MessageQueue>> GetByIdsAsync(List<string> ids)
        {
            return await Context.MessageQueue
                .Include(m => m.SecurityItems)
                .Where(i => ids.Contains(i.Id)).ToListAsync();
        }

        public async Task DeleteByIdAsync(string id)
        {
            var messageHubClient = await Context.MessageQueue
                .Include(h => h.SecurityItems)
                .FirstOrDefaultAsync(i => i.Id == id);
            if (messageHubClient != null)
            {
                Context.MessageQueue.Remove(messageHubClient);
                await Context.SaveChangesAsync();
            }
        }
    }
}
