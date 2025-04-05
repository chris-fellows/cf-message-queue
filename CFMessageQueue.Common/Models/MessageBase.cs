namespace CFMessageQueue.Models
{
    public abstract class MessageBase
    {
        /// <summary>
        /// Unique Id
        /// </summary>
        public string Id { get; set; } = String.Empty;

        /// <summary>
        /// Message type Id
        /// </summary>
        public string TypeId { get; set; } = String.Empty;

        /// <summary>
        /// Response (if any)
        /// </summary>
        public MessageResponse? Response { get; set; }        

        /// <summary>
        /// Security key for client.
        /// 
        /// Property only set for client request
        /// </summary>
        public string SecurityKey { get; set; } = String.Empty;

        /// <summary>
        /// Session Id for client.
        /// 
        /// Property only set for client request
        /// </summary>
        public string ClientSessionId { get; set; } = String.Empty;
    }
}
