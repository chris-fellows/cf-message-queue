using CFMessageQueue.Interfaces;
using CFMessageQueue.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CFMessageQueue.Services
{
    public class XmlMessageQueueService : XmlEntityWithIdService<MessageQueue, string>, IMessageQueueService
    {
        public XmlMessageQueueService(string folder) : base(folder,
                                                "MessageQueue.*.xml",
                                              (messageQueue) => $"MessageQueue.{messageQueue.Id}.xml",
                                                (messageQueueId) => $"MessageQueue.{messageQueueId}.xml")                                                
        {

        }
    }
}
