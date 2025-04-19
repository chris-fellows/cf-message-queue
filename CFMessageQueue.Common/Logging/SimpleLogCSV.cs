using System.Diagnostics;

namespace CFMessageQueue.Logs
{
    public class SimpleLogCSV : ISimpleLog
    {
        private readonly string _logFile = String.Empty;

        public SimpleLogCSV(string logFile)
        {
            _logFile = logFile;

        }

        public void Log(DateTimeOffset date, string type, string message)
        {
            var logFile = GetLogFile(date);

            var logFolder = Path.GetDirectoryName(logFile);
            if (!Directory.Exists(logFolder))
            {
                Directory.CreateDirectory(logFolder);
            }

            bool logged = false;
            var stopwatch = new Stopwatch();            
            do
            {
                try
                {
                    Char delimiter = (Char)9;
                    using (var streamWriter = new StreamWriter(logFile, true, System.Text.Encoding.UTF8))
                    {
                        streamWriter.WriteLine($"{date}{delimiter}{type}{delimiter}{message}");
                        streamWriter.Flush();
                    }
                    logged = true;
                }
                catch (Exception exception)
                {
                    if (IsFileInUseByAnotherProcess(exception))
                    {
                        if (!stopwatch.IsRunning)
                        {
                            stopwatch.Start();
                        }
                        else if (stopwatch.Elapsed >= TimeSpan.FromSeconds(5))      // Time out
                        {
                            throw;
                        }
                        Thread.Sleep(200);  // Wait before retry
                    }
                    else
                    {
                        throw;
                    }
                }
            } while (!logged);
        }

        private string GetLogFile(DateTimeOffset date)
        {
            return _logFile.Replace("{date}", date.ToString("yyyy-MM-dd"));
        }

        private static bool IsFileInUseByAnotherProcess(Exception exception)
        {
            return exception != null && exception.Message.Contains("another process");
        }
    }
}
