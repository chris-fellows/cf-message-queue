using Microsoft.Extensions.Hosting;

namespace CFMessageQueue.Hub.Models
{
    public class SystemConfig
    {
        /// <summary>
        /// Hub client port
        /// </summary>
        public int LocalPort { get; set; }


        public int MinQueuePort { get; set; }

        public int MaxQueuePort { get; set; }

        /// <summary>
        /// Admin security key. This is needed to create the initial hub client
        /// </summary>
        public string AdminSecurityKey { get; set; } = String.Empty;   //= "5005db05-35eb-4471-bd05-7883b746b196";        
    }
}
