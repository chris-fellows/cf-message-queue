using CFMessageQueue.Interfaces;
using CFMessageQueue.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CFMessageQueue.Services
{
    public class XmlQueueMessageService : XmlEntityWithIdService<QueueMessage, string>, IQueueMessageService
    {
        public XmlQueueMessageService(string folder) : base(folder,
                                                "QueueMessage.*.xml",
                                              (queueMessage) => $"QueueMessage.{queueMessage.Id}.xml",
                                                (queueMessageId) => $"QueueMessage.{queueMessageId}.xml")
        {

        }

        public async Task<List<QueueMessage>> GetExpired(string messageQueueId,DateTimeOffset now)
        {
            // Obviously not very efficient
            var queueMessages = (await GetAllAsync())
                                .Where(m => m.MessageQueueId == messageQueueId)
                                .Where(m => m.ExpirySeconds > 0 && m.CreatedDateTime.AddSeconds(m.ExpirySeconds) <= now).ToList();

            return queueMessages;
        }
    }
}
