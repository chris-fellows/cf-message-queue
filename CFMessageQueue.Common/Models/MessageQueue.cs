namespace CFMessageQueue.Models
{
    /// <summary>
    /// Message queue
    /// </summary>
    public class MessageQueue
    {
        /// <summary>
        /// Unique Id
        /// </summary>
        public string Id { get; set; } = String.Empty;

        /// <summary>
        /// Queue name
        /// </summary>
        public string Name { get; set; } = String.Empty;

        /// <summary>
        /// Queue IP
        /// </summary>
        public string Ip { get; set; } = String.Empty;

        /// <summary>
        /// Queue port
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// Max concurrent messages that can be processed. If 1 then it ensures that messages must be processed
        /// in receive order
        /// </summary>
        public int MaxConcurrentProcessing { get; set; }   
        
        /// <summary>
        /// Max number of queue items (0=No limit)
        /// </summary>
        public int MaxSize { get; set; }

        /// <summary>
        /// Queue permissions
        /// </summary>
        public List<SecurityItem> SecurityItems { get; set; } = new();
    }
}
