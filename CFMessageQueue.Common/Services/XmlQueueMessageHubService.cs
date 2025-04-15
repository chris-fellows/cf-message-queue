//using CFMessageQueue.Interfaces;
//using CFMessageQueue.Models;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace CFMessageQueue.Services
//{
//    public class XmlQueueMessageHubService : XmlEntityWithIdService<QueueMessageHub, string>, IQueueMessageHubService
//    {
//        public XmlQueueMessageHubService(string folder) : base(folder,
//                                                "QueueMessageHub.*.xml",
//                                              (queueMessageHub) => $"QueueMessageHub.{queueMessageHub.Id}.xml",
//                                                (queueMessageHubId) => $"QueueMessageHub.{queueMessageHubId}.xml")
//        {

//        }
//    }
//}
