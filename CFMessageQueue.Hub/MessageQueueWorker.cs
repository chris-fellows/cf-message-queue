using CFConnectionMessaging.Models;
using CFMessageQueue.Hub.Enums;
using CFMessageQueue.Hub.Models;
using CFMessageQueue.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace CFMessageQueue.Hub
{
    /// <summary>
    /// Worker that handles single message queue.
    /// </summary>
    public class MessageQueueWorker
    {
        private readonly MessageQueueClientsConnection _messageClientConnection;

        private ConcurrentQueue<QueueItem> _queueItems = new();

        private readonly System.Timers.Timer _timer;

        private class QueueItemTask
        {
            public Task Task { get; internal set; }

            public QueueItem QueueItem { get; internal set; }

            public QueueItemTask(Task task, QueueItem queueItem)
            {
                Task = task;
                QueueItem = queueItem;
            }
        }

        private List<QueueItemTask> _queueItemTasks = new List<QueueItemTask>();

        private readonly MessageQueue _messageQueue;

        public MessageQueueWorker(MessageQueue messageQueue, IServiceProvider serviceProvider)
        {
            _messageQueue = messageQueue;

            _messageClientConnection = new MessageQueueClientsConnection(serviceProvider);            
            _messageClientConnection.OnConnectionMessageReceived += delegate (ConnectionMessage connectionMessage, MessageReceivedInfo messageReceivedInfo)
            {
                var queueItem = new QueueItem()
                {
                    ItemType = QueueItemTypes.ConnectionMessage,
                    ConnectionMessage = connectionMessage,
                    MessageReceivedInfo = messageReceivedInfo
                };
                _queueItems.Enqueue(queueItem);                
            };

            _timer = new System.Timers.Timer();
            _timer.Elapsed += _timer_Elapsed;
            _timer.Interval = 5000;
            _timer.Enabled = false;
        }

        public void Start()
        {
            //_log.Log(DateTimeOffset.UtcNow, "Information", "Worker starting");

            _timer.Enabled = true;

            _messageClientConnection.StartListening(_messageQueue.Port);
        }

        public void Stop()
        {
           // _log.Log(DateTimeOffset.UtcNow, "Information", "Worker stopping");

            _timer.Enabled = false;

            _messageClientConnection.StopListening();
        }

        private void _timer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                _timer.Enabled = false;

                ProcessQueueItems(() =>
                {
                    // Periodic action to do while processing queue items
                });
            }
            catch(Exception exception)
            {

            }
            finally
            {
                _timer.Interval = _queueItems.Any() ||
                            _queueItemTasks.Any() ? 100 : 5000;
                _timer.Enabled = true;
            }
        }

        private void ProcessQueueItems(Action periodicAction)
        {
            while (_queueItems.Any())
            {                
                    if (_queueItems.TryDequeue(out QueueItem queueItem))
                    {
                        ProcessQueueItem(queueItem);
                    }             

                    periodicAction();
            }
        }

        private void ProcessQueueItem(QueueItem queueItem)
        {
            var queueItemTask = queueItem.ItemType switch
            {
                 QueueItemTypes.ConnectionMessage => new QueueItemTask(_messageClientConnection.HandleConnectionMessageAsync(queueItem.ConnectionMessage, queueItem.MessageReceivedInfo)),
                _ => null
            };

            if (queueItemTask != null) _queueItemTasks.Add(queueItemTask);
        }
    }
}
