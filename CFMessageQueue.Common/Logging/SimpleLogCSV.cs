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

            Char delimiter = (Char)9;
            using (var streamWriter = new StreamWriter(logFile, true, System.Text.Encoding.UTF8))
            {
                streamWriter.WriteLine($"{date}{delimiter}{type}{delimiter}{message}");
                streamWriter.Flush();
            }
        }

        private string GetLogFile(DateTimeOffset date)
        {
            return _logFile.Replace("{date}", date.ToString("yyyy-MM-dd"));
        }
    }
}
