using Microsoft.Extensions.Hosting;

namespace CFMessageQueue.Hub.Models
{
    public class SystemConfig
    {
        /// <summary>
        /// Hub client port
        /// </summary>
        public int LocalPort { get; set; }

        /// <summary>
        /// Min port for new queues
        /// </summary>
        public int MinQueuePort { get; set; }

        /// <summary>
        /// Max port for new queues
        /// </summary>
        public int MaxQueuePort { get; set; }

        /// <summary>
        /// Max days to keep log
        /// </summary>
        public int MaxLogDays { get; set; }

        public string LogFolder { get; set; } = String.Empty;

        /// <summary>
        /// Admin security key. This is needed to create the initial hub client
        /// </summary>
        public string AdminSecurityKey { get; set; } = String.Empty;   //= "5005db05-35eb-4471-bd05-7883b746b196";        
    }
}
