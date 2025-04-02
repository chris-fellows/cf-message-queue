using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Runtime.Loader;
using CFMessageQueue.Enums;
using CFMessageQueue.Hub.Models;
using CFMessageQueue.Hub;
using CFMessageQueue.Interfaces;
using System.Xml.Serialization;
using CFMessageQueue.Services;
using CFMessageQueue.Logs;
using CFMessageQueue.Models;

internal static class Program
{
    private static void Main(string[] args)
    {
        var id = Guid.NewGuid().ToString();        

        var hostEntry = Dns.GetHostEntry(Dns.GetHostName());
        Console.WriteLine($"Starting CF Message Queue Hub ({hostEntry.AddressList[0].ToString()})");

        var systemConfig = GetSystemConfig();

        var serviceProvider = CreateServiceProvider();

        // Get services
        var messageHubClientService = serviceProvider.GetRequiredService<IMessageHubClientService>();
        var messageQueueService = serviceProvider.GetRequiredService<IMessageQueueService>();
        var queueMessageHubService = serviceProvider.GetRequiredService<IQueueMessageHubService>();

        const string adminSecurityKey = "5005db05-35eb-4471-bd05-7883b746b196";
        //const string defaultSecurityKey = "0b38818c-4354-43f5-a750-a24378d2e3a8";

        // Get message hub clients (if any), create default admin client if required
        var messageHubClients = messageHubClientService.GetAllAsync().Result;
        if (!messageHubClients.Any())       // Create default admin client
        {
            // Add admin client
            var messageHubClient = new MessageHubClient()
            {
                Id = Guid.NewGuid().ToString(),
                SecurityKey = adminSecurityKey                
            };
            messageHubClientService.AddAsync(messageHubClient).Wait();
            messageHubClients.Add(messageHubClient);
        }
        var adminMessageHubClient = messageHubClients.First(c => c.SecurityKey == adminSecurityKey);

        // Check for queue message hubs. Should just be one entity as we only know about this hub
        var queueMessageHubs = queueMessageHubService.GetAllAsync().Result;
        if (!queueMessageHubs.Any())   // Create default queue message hub
        {
            var queueMessageHub = GetDefaultQueueMessageHub(systemConfig, adminMessageHubClient);
            queueMessageHubService.AddAsync(queueMessageHub).Wait();
            queueMessageHubs.Add(queueMessageHub);
        }        

        // Create message queue worker for each message queue
        var messageQueues = messageQueueService.GetAllAsync().Result;       
        var messageQueueWorkers = new List<MessageQueueWorker>();        
        foreach(var messageQueue in messageQueues)
        {
            messageQueueWorkers.Add(new MessageQueueWorker(messageQueue, serviceProvider));
        }

        // Start hub worker
        var messageHubWorker = new MessageHubWorker(queueMessageHubs.First(), serviceProvider, messageQueueWorkers);
        messageHubWorker.Start();

        // Start queue workers
        messageQueueWorkers.ForEach(worker => worker.Start());

        // Wait for requrest to stop            
        if (IsInDockerContainer)
        {
            bool active = true;
            //AssemblyLoadContext.Default.Unloading += delegate (AssemblyLoadContext context)
            //{
            //    Console.WriteLine("Stopping worker due to terminating");
            //    messageQueueWorkers.ForEach(worker => worker.Stop());
            //    Console.WriteLine("Stopped worker");
            //    active = false;
            //};

            while (active)
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
                while (!Console.KeyAvailable)
                {
                    Thread.Sleep(100);
                    Thread.Yield();
                }
            } while (Console.ReadKey(true).Key != ConsoleKey.Escape);

            // Stop hub worker
            messageHubWorker.Stop();

            // Stop queue workers
            messageQueueWorkers.ForEach(worker => worker.Stop());
        }

        Console.WriteLine("Terminated Starting CF Message Queue Hub");
    }

    private static QueueMessageHub GetDefaultQueueMessageHub(SystemConfig systemConfig, MessageHubClient adminMessageHubClient)
    {
        var queueMessageHub = new QueueMessageHub()
        {
            Id = Guid.NewGuid().ToString(),
            Ip = GetLocalIp(),
            Port = systemConfig.LocalPort,
            SecurityItems = new List<SecurityItem>()
                {
                    // Add default security item for managing admin functions
                    new SecurityItem()
                    {
                        MessageHubClientId = adminMessageHubClient.Id,
                        RoleTypes = new List<RoleTypes>()
                        {
                            RoleTypes.Admin,
                            RoleTypes.GetMessageHubs,
                            RoleTypes.GetMessageQueues
                        }
                    }
                }
        };

        return queueMessageHub;
    }

    private static string GetLocalIp()
    {
        var hostEntry = Dns.GetHostEntry(Dns.GetHostName());
        return hostEntry.AddressList[0].ToString();
    }

    private static void Default_Unloading(AssemblyLoadContext obj)
    {
        throw new NotImplementedException();
    }

    private static SystemConfig GetSystemConfig()
    {            
        return new SystemConfig()
        {
            LocalPort = Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["LocalPort"].ToString())
        };
    }

    private static bool IsInDockerContainer
    {
        get { return Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true"; }
    }

    private static IServiceProvider CreateServiceProvider()
    {
        var configFolder = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "Config");
        var logFolder = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "Log");
                
        var configuration = new ConfigurationBuilder()                       
            .Build();

        var serviceProvider = new ServiceCollection()
              .AddScoped<IMessageHubClientService>((scope) =>
              {
                  return new XmlMessageHubClientService(Path.Combine(configFolder, "MessageHubClient"));
              })
             .AddScoped<IMessageQueueService>((scope) =>
             {
                 return new XmlMessageQueueService(Path.Combine(configFolder, "MessageQueue"));
             })
             .AddScoped<IQueueMessageService>((scope) =>
             {
                return new XmlQueueMessageService(Path.Combine(configFolder, "QueueMessage"));
             })
              .AddScoped<IQueueMessageHubService>((scope) =>
              {
                  return new XmlQueueMessageHubService(Path.Combine(configFolder, "QueueMessageHub"));
              })

              // Add logging (Console & CSV)
              .AddScoped<ISimpleLog>((scope) =>
              {
                    return new SimpleMultiLog(new() {
                        new SimpleConsoleLog(),
                        new SimpleLogCSV(Path.Combine(logFolder, "MessageQueueHub-{date}.txt"))
                    });
              })

            .BuildServiceProvider();

        return serviceProvider;
    }

    /// <summary>
    /// Registers all types implementing interface
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="services"></param>
    /// <param name="assemblies"></param>
    /// <param name="lifetime"></param>
    private static IServiceCollection RegisterAllTypes<T>(this IServiceCollection services, IEnumerable<Assembly> assemblies, ServiceLifetime lifetime = ServiceLifetime.Transient)
    {
        var typesFromAssemblies = assemblies.SelectMany(a => a.DefinedTypes.Where(x => x.GetInterfaces().Contains(typeof(T))));
        foreach (var type in typesFromAssemblies)
        {
            services.Add(new ServiceDescriptor(typeof(T), type, lifetime));
        }

        return services;
    }
}