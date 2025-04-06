using CFMessageQueue.Models;
using System;
using System.Collections.Generic;
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
        }
        public void LogQueue(DateTimeOffset date, string action, string messageQueueName)
        {
            var logFile = GetLogFile(date);

            var logFolder = Path.GetDirectoryName(logFile);
            if (!Directory.Exists(logFolder))
            {
                Directory.CreateDirectory(logFolder);
            }

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
        }

        private string GetLogFile(DateTimeOffset date)
        {
            return _logFile.Replace("{date}", date.ToString("yyyy-MM-dd"));
        }
    }
}
