namespace CFMessageQueue.Models
{
    /// <summary>
    /// New queue message (External)
    /// </summary>
    public class NewQueueMessage
    {        
        /// <summary>
        /// Message type
        /// </summary>        
        public string TypeId { get; set; } = String.Empty;

        /// <summary>
        /// Message priority for processing. 0=Highest priority.
        /// 
        /// Normal messages should be sent with a non-zero priority so that high priority messages can be forced to the front
        /// of the queue.
        /// </summary>        
        public short Priority { get; set; } = 50;                

        /// <summary>
        /// Message name (User friendly description)
        /// </summary>        
        public string Name { get; set; } = String.Empty;        

        /// <summary>
        /// Expiry time (Seconds)
        /// </summary>
        public long ExpirySeconds { get; set; }

        /// <summary>
        /// Message content
        /// </summary>
        public object? Content { get; set; }

        public const short HighestPriority = 0;

        public const short LowestPriority = 100;
    }
}
