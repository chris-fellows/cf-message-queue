//using CFMessageQueue.Interfaces;
//using CFMessageQueue.Models;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace CFMessageQueue.Services
//{
//    public class XmlMessageQueueSubscriptionService : XmlEntityWithIdService<MessageQueueSubscription, string>, IMessageQueueSubscriptionService
//    {
//        public XmlMessageQueueSubscriptionService(string folder) : base(folder,
//                                                "MessageQueueSubscription.*.xml",
//                                              (messageQueueSubscription) => $"MessageQueueSubscription.{messageQueueSubscription.Id}.xml",
//                                                (messageQueueSubscriptionId) => $"MessageQueueSubscription.{messageQueueSubscriptionId}.xml")
//        {

//        }
//    }
//}
