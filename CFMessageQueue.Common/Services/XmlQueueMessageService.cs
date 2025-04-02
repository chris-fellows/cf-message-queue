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
    }
}
