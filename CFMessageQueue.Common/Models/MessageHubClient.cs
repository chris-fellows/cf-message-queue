namespace CFMessageQueue.Models
{
    /// <summary>
    /// Message hub client
    /// </summary>
    public class MessageHubClient
    {
        public string Id { get; set; } = String.Empty;

        /// <summary>
        /// Security key for accessing message hubs
        /// </summary>
        public string SecurityKey { get; set; } = String.Empty;
    }
}
