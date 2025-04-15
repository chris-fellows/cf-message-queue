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
    public class EFQueueMessageHubService : EFBaseService, IQueueMessageHubService
    {
        public EFQueueMessageHubService(IDbContextFactory<CFMessageQueueDataContext> dbFactory) : base(dbFactory)
        {

        }

        public List<QueueMessageHub> GetAll()
        {
            return Context.QueueMessageHub
                .Include(h => h.SecurityItems)
                .ToList();
        }

        public async Task<List<QueueMessageHub>> GetAllAsync()
        {
            return await Context.QueueMessageHub
                .Include(h => h.SecurityItems)
                .ToListAsync();
        }

        public async Task<QueueMessageHub> AddAsync(QueueMessageHub messageHubClient)
        {
            Context.QueueMessageHub.Add(messageHubClient);
            await Context.SaveChangesAsync();
            return messageHubClient;
        }

        public async Task<QueueMessageHub> UpdateAsync(QueueMessageHub messageHubClient)
        {
            Context.QueueMessageHub.Update(messageHubClient);
            await Context.SaveChangesAsync();
            return messageHubClient;
        }

        public async Task<QueueMessageHub?> GetByIdAsync(string id)
        {
            var messageHubClient = await Context.QueueMessageHub
                .Include(h => h.SecurityItems)
                .FirstOrDefaultAsync(i => i.Id == id);
            return messageHubClient;
        }

        public async Task<List<QueueMessageHub>> GetByIdsAsync(List<string> ids)
        {
            return await Context.QueueMessageHub
                .Include(h => h.SecurityItems)
                .Where(i => ids.Contains(i.Id)).ToListAsync();
        }

        public async Task DeleteByIdAsync(string id)
        {
            var messageHubClient = await Context.QueueMessageHub
                .Include(h => h.SecurityItems)
                .FirstOrDefaultAsync(i => i.Id == id);
            if (messageHubClient != null)
            {
                Context.QueueMessageHub.Remove(messageHubClient);
                await Context.SaveChangesAsync();
            }
        }
    }
}
