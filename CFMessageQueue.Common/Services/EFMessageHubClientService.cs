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
    public class EFMessageHubClientService : EFBaseService, IMessageHubClientService
    {
        public EFMessageHubClientService(IDbContextFactory<CFMessageQueueDataContext> dbFactory) : base(dbFactory)
        {

        }

        public List<MessageHubClient> GetAll()
        {
            return Context.MessageHubClient                
                .ToList();
        }

        public async Task<List<MessageHubClient>> GetAllAsync()
        {
            return await Context.MessageHubClient                
                .ToListAsync();
        }

        public async Task<MessageHubClient> AddAsync(MessageHubClient messageHubClient)
        {
            Context.MessageHubClient.Add(messageHubClient);
            await Context.SaveChangesAsync();
            return messageHubClient;
        }

        public async Task<MessageHubClient> UpdateAsync(MessageHubClient messageHubClient)
        {
            Context.MessageHubClient.Update(messageHubClient);
            await Context.SaveChangesAsync();
            return messageHubClient;
        }

        public async Task<MessageHubClient?> GetByIdAsync(string id)
        {
            var messageHubClient = await Context.MessageHubClient                
                .FirstOrDefaultAsync(i => i.Id == id);
            return messageHubClient;
        }

        public async Task<List<MessageHubClient>> GetByIdsAsync(List<string> ids)
        {
            return await Context.MessageHubClient                
                .Where(i => ids.Contains(i.Id)).ToListAsync();
        }

        public async Task DeleteByIdAsync(string id)
        {
            var messageHubClient = await Context.MessageHubClient                
                .FirstOrDefaultAsync(i => i.Id == id);
            if (messageHubClient != null)
            {
                Context.MessageHubClient.Remove(messageHubClient);
                await Context.SaveChangesAsync();
            }
        }     
    }
}
