using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CFMessageQueue.TestClient.Extensions
{
    internal static class StopwatchExtensions
    {
        public static void Wait(this Stopwatch stopwatch, TimeSpan delay, CancellationToken cancellationToken)
        {
            stopwatch.Restart();
            while (stopwatch.Elapsed < delay &&
                !cancellationToken.IsCancellationRequested)
            {
                Thread.Sleep(1);
            }
        }
    }
}
