//using System;
//using System.Collections.Generic;
//using System.ComponentModel.DataAnnotations;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace CFMessageQueue.Models
//{
//    public class MessageQueueV2
//    {
//        /// <summary>
//        /// Unique Id
//        /// </summary>
//        [MaxLength(50)]
//        public string Id { get; set; } = String.Empty;

//        /// <summary>
//        /// Queue name
//        /// </summary>
//        [MaxLength(100)]
//        public string Name { get; set; } = String.Empty;

//        /// <summary>
//        /// Queue IP
//        /// </summary>
//        [MaxLength(10)]
//        public string Ip { get; set; } = String.Empty;

//        /// <summary>
//        /// Queue port
//        /// </summary>
//        public int Port { get; set; }

//        /// <summary>
//        /// Max concurrent messages that can be processed. If 1 then it ensures that messages must be processed
//        /// in receive order
//        /// </summary>
//        public int MaxConcurrentProcessing { get; set; }

//        /// <summary>
//        /// Max number of queue items (0=No limit)
//        /// </summary>
//        public int MaxSize { get; set; }

//        /// <summary>
//        /// Queue permissions
//        /// </summary>
//        //public List<SecurityItem> SecurityItems { get; set; } = new();
//        public ICollection<SecurityItem> SecurityItems { get; set; }
//    }
//}
