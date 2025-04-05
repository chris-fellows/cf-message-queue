namespace CFMessageQueue.Models
{
    public class MessageQueue
    {
        public string Id { get; set; } = String.Empty;

        public string Name { get; set; } = String.Empty;

        public string Ip { get; set; } = String.Empty;

        public int Port { get; set; }

        /// <summary>
        /// Max concurrent messages that can be processed. If 1 then it ensures that messages must be processed
        /// in receive order
        /// </summary>
        public int MaxConcurrentProcessing { get; set; }   
        
        public int MaxSize { get; set; }

        public List<SecurityItem> SecurityItems { get; set; } = new();
    }
}
