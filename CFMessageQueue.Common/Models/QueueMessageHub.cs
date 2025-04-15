using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CFMessageQueue.Models
{
    /// <summary>
    /// Queue message hub details
    /// </summary>
    public class QueueMessageHub
    {
        [MaxLength(50)]
        public string Id { get; set; } = String.Empty;

        [MaxLength(10)]
        public string Ip { get; set; } = String.Empty;

        /// <summary>
        /// Port for communicating with hub.
        /// </summary>
        [Range(1, Int32.MaxValue)]
        public int Port { get; set; }

        public ICollection<SecurityItem> SecurityItems { get; set; }
    }
}
