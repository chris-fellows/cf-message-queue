namespace CFMessageQueue.Logs
{
    public interface ISimpleLog
    {
        void Log(DateTimeOffset date, string type, string message);
    }
}
