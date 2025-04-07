using CFConnectionMessaging.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CFMessageQueue.TestClient.Models
{
    internal class SystemConfig
    {
        public static EndpointInfo HubEndpointInfo { get; set; } = new EndpointInfo() { Ip = "192.168.1.45", Port = 10000 };


        public static string Client1Name { get; set; } = "Client 1";

        public static string Client2Name { get; set; } = "Client 2";

        public static string Client3Name { get; set; } = "Client 3";

        public static string Client1SecurityKey { get; set; } = "0b38818c-4354-43f5-a750-a24378d2e3a8";

        public static string Client2SecurityKey { get; set; } = "6c3f7a9e-3ab8-428c-864a-b01c936bccf9";

        public static string Client3SecurityKey { get; set; } = "ea0b9d0a-9f7c-4d87-908d-14260d298d81";

        /// <summary>
        /// Security key for admin functions. E.g. Create clients etc
        /// </summary>
        public static string AdminSecurityKey { get; set; } = "5005db05-35eb-4471-bd05-7883b746b196";

        ///// <summary>
        ///// Default hub security key. E.g. Get queues
        ///// </summary>
        //public static string DefaultHubSecurityKey { get; set; } = "0b38818c-4354-43f5-a750-a24378d2e3a8";

        public static string Queue1Name { get; set; } = "Queue 1";

        public static string Queue2Name { get; set; } = "Queue 2";

        ///// <summary>
        ///// Local port for hub communications
        ///// </summary>
        //public static int HubClientLocalPort { get; set; } = 10100;

        ///// <summary>
        ///// Local port for queue specific communications (Queue 1)
        ///// </summary>
        //public static int Queue1LocalPort { get; set; } = 10101;

        ///// <summary>
        ///// Local port for queue specific communications (Queue 2)
        ///// </summary>
        //public static int Queue2LocalPort { get; set; } = 10102;

        public static int MinClientLocalPort { get; set; } = 10001;

        public static int MaxClientLocalPort { get; set; } = 10050;
    }
}
