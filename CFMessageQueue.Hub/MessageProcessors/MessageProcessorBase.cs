using CFMessageQueue.Hub.Models;
using CFMessageQueue.Interfaces;
using CFMessageQueue.Logging;
using CFMessageQueue.Logs;
using CFMessageQueue.Models;
using CFMessageQueue.Utilities;
using SQLitePCL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CFMessageQueue.Hub.MessageProcessors
{
    internal abstract class MessageProcessorBase
    {
        protected readonly IAuditLog _auditLog;
        protected readonly ISimpleLog _log;
        protected readonly IServiceProvider _serviceProvider;
        protected HubResources? _hubResources;

        public MessageProcessorBase(IAuditLog auditLog,
                                ISimpleLog log,
                                IServiceProvider serviceProvider)
        {
            _auditLog = auditLog;
            _log = log;
            _serviceProvider = serviceProvider;
        }

        public void Configure(object hubResources)
        {
            _hubResources = (HubResources)hubResources;
        }

        /// <summary>
        /// Whether security key is valid format
        /// </summary>
        /// <param name="securityKey"></param>
        /// <returns></returns>
        protected static bool IsValidFormatSecurityKey(string securityKey)
        {
            return securityKey.Length > 10 && securityKey.Length <= 1024;
        }

        protected MessageHubClient? GetMessageHubClientBySecurityKey(string securityKey, IMessageHubClientService messageHubClientService)
        {
            if (_hubResources.MessageHubClientsLastRefresh.AddMinutes(5) <= DateTimeOffset.UtcNow)       // Periodic refresh
            {
                _hubResources.MessageHubClientsBySecurityKey.Clear();
            }
            if (!_hubResources.MessageHubClientsBySecurityKey.Any())   // Cache empty, load it
            {
                RefreshMessageHubClients(messageHubClientService);
            }
            return _hubResources.MessageHubClientsBySecurityKey.ContainsKey(securityKey) ? _hubResources.MessageHubClientsBySecurityKey[securityKey] : null;
        }

        protected void RefreshMessageHubClients(IMessageHubClientService messageHubClientService)
        {
            _hubResources.MessageHubClientsLastRefresh = DateTimeOffset.UtcNow;

            _hubResources.MessageHubClientsBySecurityKey.Clear();
            var messageHubClients = messageHubClientService.GetAll();
            foreach (var messageHubClient in messageHubClients)
            {
                _hubResources.MessageHubClientsBySecurityKey.TryAdd(messageHubClient.SecurityKey, messageHubClient);
            }
        }

        /// <summary>
        /// Returns a free port or 0 if none
        /// </summary>
        /// <param name="messageQueues"></param>
        /// <returns></returns>
        protected int GetFreeQueuePort(List<MessageQueue> messageQueues)
        {
            int port = _hubResources.SystemConfig.MinQueuePort - 1;

            do
            {
                port++;

                if (!messageQueues.Any(q => q.Port == port))
                {
                    if (NetworkUtilities.IsLocalPortFree(port)) return port;
                }
                else if (port >= _hubResources.SystemConfig.MaxQueuePort)
                {
                    return 0;
                }
            } while (true);
        }
    }
}
