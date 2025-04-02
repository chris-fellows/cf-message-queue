using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CFMessageQueue.Hub.Models
{
    public class QueueItemTask
    {
        public Task Task { get; internal set; }

        public QueueItem QueueItem { get; internal set; }

        public QueueItemTask(Task task, QueueItem queueItem)
        {
            Task = task;
            QueueItem = queueItem;
        }
    }
}
