namespace CFMessageQueue.Logs
{
    public class SimpleMultiLog : ISimpleLog
    {
        private readonly List<ISimpleLog> _simpleLogs;

        public SimpleMultiLog(List<ISimpleLog> simpleLogs)
        {
            _simpleLogs = simpleLogs;
        }

        public void Log(DateTimeOffset date, string type, string message)
        {
            var exceptions = new List<Exception>();

            foreach (var log in _simpleLogs)
            {
                try
                {
                    log.Log(date, type, message);
                }
                catch(Exception exception)
                {
                    exceptions.Add(exception);
                }
            }

            if (exceptions.Count > 1)
            {
                throw new AggregateException(exceptions);
            }
            else if (exceptions.Count == 1)
            {
                throw exceptions[0];
            }
        }
    }
}
