using CFMessageQueue.Data;
using Microsoft.EntityFrameworkCore;

namespace CFMessageQueue.Services
{
    /// <summary>
    /// Base EF service
    /// </summary>
    public abstract class EFBaseService : IDisposable
    {
        private readonly IDbContextFactory<CFMessageQueueDataContext> _dbFactory;
        private CFMessageQueueDataContext? _context;
        //private readonly Lazy<CFMessageQueueDataContext> _contextLazy;

        public EFBaseService(IDbContextFactory<CFMessageQueueDataContext> dbFactory)
        {
            _dbFactory = dbFactory;
            //_contextLazy = new Lazy<CFIssueTrackerContext>(() =>
            //{
            //    return _dbFactory.CreateDbContext();
            //});
        }

        /// <summary>
        /// DB context. Creates if instance not set.
        /// </summary>
        protected CFMessageQueueDataContext Context
        {
            get
            {
                lock (_dbFactory)
                {
                    if (_context == null) _context = _dbFactory.CreateDbContext();
                    return _context;
                }
            }
        }

        public void Dispose()
        {
            if (_context != null)
            {
                _context.Dispose();
                _context = null;
            }

            //if (_contextLazy
            //{
            //    _contextLazy.Value.Dispose();
            //}            
        }
    }
}
