using CFConnectionMessaging.Models;
using CFMessageQueue;
using CFMessageQueue.Models;

namespace CFMessageQueue.Common.Interfaces
{
    /// <summary>
    /// Processes message received
    /// </summary>
    public interface IMessageProcessor
    {
        /// <summary>
        /// Configure instance
        /// </summary>
        /// <param name="hubResources"></param>
        void Configure(object hubResources);

        /// <summary>
        /// Process message
        /// </summary>
        /// <param name="message"></param>
        /// <param name="messageReceivedInfo"></param>
        /// <returns></returns>
        Task ProcessAsync(MessageBase message, MessageReceivedInfo messageReceivedInfo);

        /// <summary>
        /// Whether instance can process message
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        bool CanProcess(MessageBase message);
    }
}
