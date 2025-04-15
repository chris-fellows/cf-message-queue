using System.ComponentModel.DataAnnotations;

namespace CFMessageQueue.Models
{
    /// <summary>
    /// Message hub client
    /// </summary>
    public class MessageHubClient
    {
        [MaxLength(50)]
        public string Id { get; set; } = String.Empty;

        /// <summary>
        /// Client name
        /// </summary>
        [MaxLength(100)]
        public string Name { get; set; } = String.Empty;

        /// <summary>
        /// Security key for accessing message hubs
        /// </summary>
        /// 
        [MaxLength(1000)]
        public string SecurityKey { get; set; } = String.Empty;
    }
}
