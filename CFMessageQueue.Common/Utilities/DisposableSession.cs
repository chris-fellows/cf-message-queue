namespace CFMessageQueue.Utilities
{
    /// <summary>
    /// Session that executes a list of action(s) when disposed. E.g. Clean up resources
    /// </summary>
    public class DisposableSession : IDisposable
    {
        private readonly List<Action> _actions;

        public DisposableSession(List<Action> actions)
        {
            _actions = actions;
        }

        public DisposableSession()
        {
            _actions = new();
        }

        public void Add(Action action)
        {
            _actions.Add(action);
        }

        public void Dispose()
        {
            var exceptions = new List<Exception>();

            foreach (var action in _actions)
            {
                try
                {
                    action.Invoke();
                }
                catch (Exception exception)
                {
                    exceptions.Add(exception);
                }
            }

            _actions.Clear();
            if (exceptions.Count > 1)
            {
                throw new AggregateException(exceptions.ToArray());
            }
            else if (exceptions.Count == 1)
            {
                throw exceptions.First();
            }
        }
    }
}
