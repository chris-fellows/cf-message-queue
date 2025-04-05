using CFMessageQueue.Models;
using CFMessageQueue.Services;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CFMessageQueue.TestClient
{
    internal class MessageQueueNotificationsTest
    {
        /// <summary>
        /// Handles queue notifications until cancel requested
        /// </summary>
        /// <param name="messageQueue"></param>
        /// <param name="securityKey"></param>
        /// <param name="localPort"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task HandleNotificationsAsync(MessageQueue messageQueue, string securityKey, int localPort,
                                        Action<string, long?> notificationAction,
                                        TimeSpan queueSizeNotificationFrequency,
                                        CancellationToken cancellationToken)
        {
            return Task.Factory.StartNew(async () =>
            {
                using (var messageQueueClientConnector = new MessageQueueClientConnector(securityKey, localPort))
                {
                    // Subscribe
                    await messageQueueClientConnector.SubscribeAsync((eventName, queueSize) =>
                    {
                        notificationAction(eventName, queueSize);
                    }, queueSizeNotificationFrequency);

                    // Wait until cancelled
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        Thread.Sleep(100);
                    }

                    // Unsubscribe
                    await messageQueueClientConnector.UnsubscribeAsync();
                }
            });
        }
    }
}
