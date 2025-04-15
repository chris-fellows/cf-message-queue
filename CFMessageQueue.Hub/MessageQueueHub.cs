using CFMessageQueue.Enums;
using CFMessageQueue.Hub.Models;
using CFMessageQueue.Interfaces;
using CFMessageQueue.Models;
using CFMessageQueue.Utilities;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Loader;

namespace CFMessageQueue.Hub
{
    /// <summary>
    /// Message queue hub.
    /// 
    /// Actions:
    /// - Manages hub worker to handle hub level actions. E.g. Create queue.
    /// - Manages queue worker to handle queue level actions. Get add message, get next message etc.
    /// </summary>
    internal class MessageQueueHub
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly SystemConfig _systemConfig;

        public MessageQueueHub(IServiceProvider serviceProvider, SystemConfig systemConfig)
        {
            _serviceProvider = serviceProvider;
            _systemConfig = systemConfig;
        }        

        /// <summary>
        /// Initialises message hub clients
        /// </summary>
        /// <param name="messageHubClientService"></param>
        /// <returns>Admin message hub client for this hub</returns>
        private MessageHubClient InitialiseMessageHubClients(IMessageHubClientService messageHubClientService)
        {
            // Get message hub clients (if any), create default admin client if required
            var messageHubClients = messageHubClientService.GetAllAsync().Result;

            // Ensure that message hub clients includes instance for this admin security key
            var adminMessageHubClient = messageHubClients.FirstOrDefault(c => c.SecurityKey == _systemConfig.AdminSecurityKey);
            if (adminMessageHubClient == null)
            {
                // Add admin client
                adminMessageHubClient = new MessageHubClient()
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = "Hub Admin",
                    SecurityKey = _systemConfig.AdminSecurityKey
                };
                messageHubClientService.AddAsync(adminMessageHubClient).Wait();
                messageHubClients.Add(adminMessageHubClient);
            }

            return adminMessageHubClient!;
        }

        /// <summary>
        /// Initialises message hubs list.
        /// 
        /// Currently there should only be one instance (This hub). Eventually then each hub will be aware of other hubs so that
        /// the system supports advanced functions.
        /// </summary>
        /// <param name="queueMessageHubService"></param>
        /// <param name="adminMessageHubClient"></param>
        /// <returns></returns>
        private QueueMessageHub InitialiseMessageHubs(IQueueMessageHubService queueMessageHubService, MessageHubClient adminMessageHubClient)
        {
            // Check for queue message hubs. Should just be one entity as we only know about this hub
            var queueMessageHubs = queueMessageHubService.GetAllAsync().Result;
            if (!queueMessageHubs.Any())   // Create default queue message hub
            {
                var queueMessageHub = GetDefaultQueueMessageHub(_systemConfig, adminMessageHubClient);
                queueMessageHubService.AddAsync(queueMessageHub).Wait();
                queueMessageHubs.Add(queueMessageHub);
            }

            return queueMessageHubs.First();    // This message hib
        }

        /// <summary>
        /// Gets message queue workers, one per message queue
        /// </summary>
        /// <param name="messageQueueService"></param>
        /// <returns></returns>
        private List<MessageQueueWorker> GetMessageQueueWorkers(IMessageQueueService messageQueueService)
        {
            // Create message queue worker for each message queue
            var messageQueues = messageQueueService.GetAllAsync().Result;
            var messageQueueWorkers = new List<MessageQueueWorker>();
            foreach (var messageQueue in messageQueues)
            {
                messageQueueWorkers.Add(new MessageQueueWorker(messageQueue, _serviceProvider));
            }

            return messageQueueWorkers;
        }

        /// <summary>
        /// Runs hub 
        /// </summary>
        /// <param name="cancellationToken"></param>
        public void Run(CancellationToken cancellationToken)
        {
            // Get services
            var messageHubClientService = _serviceProvider.GetRequiredService<IMessageHubClientService>();
            var messageQueueService = _serviceProvider.GetRequiredService<IMessageQueueService>();
            var queueMessageHubService = _serviceProvider.GetRequiredService<IQueueMessageHubService>();

            // Initialise message hub clients
            var adminMessageHubClient = InitialiseMessageHubClients(messageHubClientService);

            // Initialise queue message hubs
            var queueMessageHub = InitialiseMessageHubs(queueMessageHubService, adminMessageHubClient);

            // Get message queue workers, one per queue. This list will be updated when queues are created or deleted
            var messageQueueWorkers = GetMessageQueueWorkers(messageQueueService);
            
            // Start hub worker
            var messageHubWorker = new MessageHubWorker(queueMessageHub, _serviceProvider, messageQueueWorkers, _systemConfig);
            messageHubWorker.Start();

            // Start queue workers
            messageQueueWorkers.ForEach(worker => worker.Start());            
            
            // Wait for requrest to stop            
            if (IsInDockerContainer)
            {
                bool active = true;
                var loadContext = AssemblyLoadContext.GetLoadContext(typeof(Program).Assembly);
                if (loadContext != null)
                {
                    loadContext.Unloading += delegate (AssemblyLoadContext context)
                    {
                        Console.WriteLine("Detected that container is stopping");
                        active = false;
                    };
                }

                /*
                AssemblyLoadContext.Default.Unloading += delegate (AssemblyLoadContext context)
                {
                    Console.WriteLine("Stopping worker due to terminating");
                    messageQueueWorkers.ForEach(worker => worker.Stop());
                    Console.WriteLine("Stopped worker");
                    active = false;
                };
                */

                while (active &&
                    !cancellationToken.IsCancellationRequested)
                {
                    Thread.Sleep(100);
                    Thread.Yield();
                }
            }
            else
            {
                do
                {
                    Console.WriteLine("Press ESCAPE to stop");  // Also displayed if user presses other key
                    while (!Console.KeyAvailable &&
                        !cancellationToken.IsCancellationRequested)
                    {
                        Thread.Sleep(100);
                        Thread.Yield();
                    }
                } while (Console.ReadKey(true).Key != ConsoleKey.Escape &&
                        !cancellationToken.IsCancellationRequested);
            }

            // Stop hub worker
            Console.WriteLine("Stopping hub worker");
            messageHubWorker.Stop();

            // Stop queue workers
            Console.WriteLine("Stopping message queue worker");
            messageQueueWorkers.ForEach(worker => worker.Stop());
        }

        private static bool IsInDockerContainer
        {
            get { return Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true"; }
        }

        private static QueueMessageHub GetDefaultQueueMessageHub(SystemConfig systemConfig, MessageHubClient adminMessageHubClient)
        {
            var queueMessageHub = new QueueMessageHub()
            {
                Id = Guid.NewGuid().ToString(),
                Ip = NetworkUtilities.GetLocalIPV4Addresses().First(),
                Port = systemConfig.LocalPort,
                SecurityItems = new List<SecurityItem>()
                {
                    // Add default security item for managing admin functions
                    new SecurityItem()
                    {
                        Id = Guid.NewGuid().ToString(),
                        MessageHubClientId = adminMessageHubClient.Id,
                        RoleTypes = RoleTypeUtilities.DefaultAdminHubClientRoleTypes
                    }
                }
            };

            return queueMessageHub;
        }
    }
}
