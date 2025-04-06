namespace CFMessageQueue.Logs
{
    /// <summary>
    /// Simple log
    /// </summary>
    public interface ISimpleLog
    {
        void Log(DateTimeOffset date, string type, string message);
    }
}
