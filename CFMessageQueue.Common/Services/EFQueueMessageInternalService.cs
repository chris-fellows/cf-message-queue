﻿using CFMessageQueue.Data;
using CFMessageQueue.Enums;
using CFMessageQueue.Interfaces;
using CFMessageQueue.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CFMessageQueue.Services
{
    public class EFQueueMessageInternalService : EFBaseService, IQueueMessageInternalService
    {
        public EFQueueMessageInternalService(IDbContextFactory<CFMessageQueueDataContext> dbFactory) : base(dbFactory)
        {

        }

        public List<QueueMessageInternal> GetAll()
        {
            return Context.QueueMessageInternal
                .ToList();
        }

        public async Task<List<QueueMessageInternal>> GetAllAsync()
        {
            return await Context.QueueMessageInternal
                .ToListAsync();
        }

        public async Task<QueueMessageInternal> AddAsync(QueueMessageInternal messageHubClient)
        {
            Context.QueueMessageInternal.Add(messageHubClient);
            await Context.SaveChangesAsync();
            return messageHubClient;
        }

        public async Task<QueueMessageInternal> UpdateAsync(QueueMessageInternal messageHubClient)
        {
            Context.QueueMessageInternal.Update(messageHubClient);
            await Context.SaveChangesAsync();
            return messageHubClient;
        }

        public async Task<QueueMessageInternal?> GetByIdAsync(string id)
        {
            var messageHubClient = await Context.QueueMessageInternal
                .FirstOrDefaultAsync(i => i.Id == id);
            return messageHubClient;
        }

        public async Task<List<QueueMessageInternal>> GetByIdsAsync(List<string> ids)
        {
            return await Context.QueueMessageInternal
                .Where(i => ids.Contains(i.Id)).ToListAsync();
        }

        public async Task DeleteByIdAsync(string id)
        {
            var messageHubClient = await Context.QueueMessageInternal
                .FirstOrDefaultAsync(i => i.Id == id);
            if (messageHubClient != null)
            {
                Context.QueueMessageInternal.Remove(messageHubClient);
                await Context.SaveChangesAsync();
            }
        }

        public async Task<List<QueueMessageInternal>> GetExpiredAsync(string messageQueueId, DateTimeOffset now)
        {            
            return await Context.QueueMessageInternal
                .Where(i => i.MessageQueueId == messageQueueId &&                        
                        i.ExpiryDateTime <= now).ToListAsync();
        }

        public async Task<List<QueueMessageInternal>> GetByMessageQueueAsync(string messageQueueId)
        {
            return await Context.QueueMessageInternal
                .Where(i => i.MessageQueueId == messageQueueId).ToListAsync();                        
        }

        public async Task<QueueMessageInternal?> GetNextAsync(string messageQueueId)
        {
            // (m.ExpirySeconds == 0 || m.CreatedDateTime.AddSeconds(m.ExpirySeconds) < DateTimeOffset.UtcNow))
            var now = DateTime.UtcNow;

            // Calling m.CreatedDateTime.AddSeconds(m.ExpirySeconds) causes error
            return await Context.QueueMessageInternal                    
                    .Where(m => m.MessageQueueId == messageQueueId &&
                        m.Status == QueueMessageStatuses.Default &&
                        m.ExpiryDateTime >= now)
                    .OrderBy(m => m.Priority)
                    .ThenBy(m => m.CreatedDateTime).FirstOrDefaultAsync();                                        
        }
    }
}
