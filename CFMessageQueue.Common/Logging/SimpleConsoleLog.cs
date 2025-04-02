namespace CFMessageQueue.Logs
{
    public class SimpleConsoleLog : ISimpleLog
    {
        public void Log(DateTimeOffset date, string type, string message)
        {
            Console.WriteLine($"{date} {type} {message}");
        }
    }
}
