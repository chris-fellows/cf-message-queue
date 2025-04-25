using CFConnectionMessaging.Models;
using CFMessageQueue.Interfaces;

namespace CFMessageQueue.CLI.Interfaces
{
    /// <summary>
    /// Connection details service
    /// </summary>
    internal interface IConnectionService
    {
        /// <summary>
        /// Security key. Note that there are seperate keys for the hub and each queue
        /// </summary>
        string SecurityKey { get; set; }

        /// <summary>
        /// Hub location
        /// </summary>
        EndpointInfo RemoteEndpointInfo { get; set; }

        /// <summary>
        /// Message hub connection
        /// </summary>
        IMessageHubClientConnector? MessageHubClientConnector { get; set; }

        /// <summary>
        /// Message queue connection
        /// </summary>
        IMessageQueueClientConnector? MessageQueueClientConnector { get; set; }
    }
}
