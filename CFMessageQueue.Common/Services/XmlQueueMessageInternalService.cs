//using CFMessageQueue.Interfaces;
//using CFMessageQueue.Models;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace CFMessageQueue.Services
//{
//    public class XmlQueueMessageInternalService : XmlEntityWithIdService<QueueMessageInternal, string>, IQueueMessageInternalService
//    {
//        public XmlQueueMessageInternalService(string folder) : base(folder,
//                                                "QueueMessageInternal.*.xml",
//                                              (queueMessageInternal) => $"QueueMessageInternal.{queueMessageInternal.Id}.xml",
//                                                (queueMessageInternal) => $"QueueMessageInternal.{queueMessageInternal}.xml")
//        {

//        }

//        public async Task<List<QueueMessageInternal>> GetExpiredAsync(string messageQueueId, DateTimeOffset now)
//        {
//            // Obviously not very efficient
//            var queueMessages = (await GetAllAsync())
//                                .Where(m => m.MessageQueueId == messageQueueId)
//                                .Where(m => m.ExpirySeconds > 0 && m.CreatedDateTime.AddSeconds(m.ExpirySeconds) <= now).ToList();

//            return queueMessages;
//        }

//        public async Task<List<QueueMessageInternal>> GetByMessageQueueAsync(string messageQueueId)
//        {
//            // Obviously not very efficient
//            var queueMessages = (await GetAllAsync())
//                                .Where(m => m.MessageQueueId == messageQueueId).ToList();                                

//            return queueMessages;
//        }
//    }
//}
