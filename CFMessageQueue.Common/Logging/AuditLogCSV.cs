using CFMessageQueue.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CFMessageQueue.Logging
{
    public class AuditLogCSV : IAuditLog
    {
        private readonly string _logFile = String.Empty;

        public AuditLogCSV(string logFile)
        {
            _logFile = logFile;
        }

        public void LogQueueMessage(DateTimeOffset date, string action, 
                            string messageQueueName, QueueMessageInternal queueMessage)
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
                    var isWriteHeaders = !File.Exists(logFile);
                    using (var streamWriter = new StreamWriter(logFile, true, System.Text.Encoding.UTF8))
                    {
                        if (isWriteHeaders)
                        {
                            streamWriter.WriteLine($"Date{delimiter}Action{delimiter}Queue{delimiter}MessageId{delimiter}MessageTypeId{delimiter}CreatedDateTime");
                        }

                        streamWriter.WriteLine($"{date}{delimiter}{action}{delimiter}{messageQueueName}{delimiter}{queueMessage.Id}{delimiter}{queueMessage.TypeId}{delimiter}{queueMessage.CreatedDateTime}");
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

        private static bool IsFileInUseByAnotherProcess(Exception exception)
        {
            return exception != null && exception.Message.Contains("another process");
        }

        public void LogQueue(DateTimeOffset date, string action, string messageQueueName)
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
                    var isWriteHeaders = !File.Exists(logFile);
                    using (var streamWriter = new StreamWriter(logFile, true, System.Text.Encoding.UTF8))
                    {
                        if (isWriteHeaders)
                        {
                            streamWriter.WriteLine($"Date{delimiter}Action{delimiter}Queue{delimiter}MessageId{delimiter}MessageTypeId{delimiter}CreatedDateTime");
                        }

                        streamWriter.WriteLine($"{date}{delimiter}{action}{delimiter}{messageQueueName}{delimiter}{""}{delimiter}{""}{delimiter}{""}");
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
    }
}
