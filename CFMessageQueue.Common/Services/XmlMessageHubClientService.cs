using CFMessageQueue.Interfaces;
using CFMessageQueue.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CFMessageQueue.Services
{
    public class XmlMessageHubClientService : XmlEntityWithIdService<Models.MessageHubClient, string>, IMessageHubClientService
    {
        public XmlMessageHubClientService(string folder) : base(folder,
                                                "MessageHubClient.*.xml",
                                              (messageHubClient) => $"MessageHubClient.{messageHubClient.Id}.xml",
                                                (messageHubClientId) => $"MessageHubClient.{messageHubClientId}.xml")
        {

        }
    }
}
