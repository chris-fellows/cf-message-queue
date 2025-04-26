using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using CFMessageQueue.Hub.Models;
using CFMessageQueue.Hub;
using CFMessageQueue.Interfaces;
using CFMessageQueue.Services;
using CFMessageQueue.Logs;
using CFMessageQueue.Utilities;
using CFMessageQueue.Logging;
using CFMessageQueue.Data;
using Microsoft.EntityFrameworkCore;
using CFMessageQueue.Common.Interfaces;

internal static class Program
{
    private static void Main(string[] args)
    {
        Console.WriteLine($"Starting CF Message Queue Hub ({NetworkUtilities.GetLocalIPV4Addresses()[0]})");

        // Get system config
        var systemConfig = GetSystemConfig(args);

        // Get service provider
        var serviceProvider = CreateServiceProvider(systemConfig);

        CreateDatabase(serviceProvider);

        // Create message queue hub
        var messageQueueHub = new MessageQueueHub(serviceProvider, systemConfig);

        // Message hub until shutdown requested
        var cancellationTokenSource = new CancellationTokenSource();
        messageQueueHub.Run(cancellationTokenSource.Token);

        Console.WriteLine("Terminated Starting CF Message Queue Hub");
    }

    //private static bool IsInDockerContainer
    //{
    //    get { return Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true"; }
    //}

    private static void CreateDatabase(IServiceProvider serviceProvider)
    {
        using (var context = serviceProvider.GetRequiredService<CFMessageQueueDataContext>())
        {
            context.Database.EnsureCreated();
        }        
    }

    //private static void CreateDatabaseWithTestData(IServiceProvider serviceProvider)
    //{        
    //    using (var context = serviceProvider.GetRequiredService<CFMessageQueueDataContext>())
    //    {
    //        context.Database.EnsureCreated();

    //        // Add clients
    //        var messageHubClients = new List<MessageHubClient>();
    //        for(int index =0; index < 5; index++)
    //        {
    //            var client = new MessageHubClient()
    //            {
    //                Id = Guid.NewGuid().ToString(),
    //                Name = $"Client {index + 1}",
    //                SecurityKey = Guid.NewGuid().ToString() + "-" + Guid.NewGuid().ToString()
    //            };
    //            messageHubClients.Add(client);

    //            context.MessageHubClient.Add(client);
    //        }

    //        // Add message queue
    //        var messageQueueV2 = new MessageQueue()
    //        {
    //            Id = Guid.NewGuid().ToString(),
    //            Ip = "10",
    //            Name = "Message Queue 1",
    //            SecurityItems = new List<SecurityItem>()
    //            {
    //                new SecurityItem()
    //                {
    //                    Id = Guid.NewGuid().ToString(),
    //                    MessageHubClientId = Guid.NewGuid().ToString(),
    //                    RoleTypes = new List<RoleTypes>() { RoleTypes.QueueReadQueue, RoleTypes.HubAdmin }
    //                }
    //            }
    //        };

    //        context.MessageQueue.Add(messageQueueV2);

    //        var messageHubClient = messageHubClients.First();

    //        for (int index =0; index < 50; index++)
    //        {
    //            var message = new QueueMessageInternal()
    //            {
    //                Id = Guid.NewGuid().ToString(),
    //                Content = new byte[10000],
    //                ContentType = "Test",
    //                CreatedDateTime = DateTimeOffset.UtcNow,
    //                ExpirySeconds = 6000,
    //                MaxProcessingMilliseconds = 20,
    //                MessageQueueId = messageQueueV2.Id,
    //                Name = $"Test message {index + 1}",
    //                //ProcessingMessageHubClientId = Guid.NewGuid().ToString(),
    //                ProcessingStartDateTime = DateTimeOffset.MinValue,
    //                SenderMessageHubClientId = messageHubClient.Id,
    //                Status = CFMessageQueue.Enums.QueueMessageStatuses.Processed,
    //                TypeId = Guid.NewGuid().ToString()
    //            };

    //            context.QueueMessageInternal.Add(message);
    //        }                  

    //        context.SaveChanges();

    //        int xxx = 1000;
    //    }

    //    using (var context = serviceProvider.GetRequiredService<CFMessageQueueDataContext>())
    //    {
    //        var clients = context.MessageHubClient.ToList();
          
    //        int xxx = 1000;
    //    }
    //}

    /// <summary>
    /// Gets system config. Default from config, can be overridden by command line args
    /// </summary>
    /// <param name="args"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    private static SystemConfig GetSystemConfig(string[] args)
    {            
        var systemConfig = new SystemConfig()
        {
            LocalPort = Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["LocalPort"].ToString()),
            MinQueuePort = Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["MinQueuePort"].ToString()),
            MaxQueuePort = Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["MaxQueuePort"].ToString()),
            MaxLogDays = Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["MaxLogDays"].ToString()),
            LogFolder = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "Log"),
            AdminSecurityKey = System.Configuration.ConfigurationManager.AppSettings["AdminSecurityKey"].ToString()            
        };

        // Override with arguments
        foreach (var arg in args)
        {            
            if (arg.ToLower().StartsWith("-adminsecuritykey="))
            {
                systemConfig.AdminSecurityKey = arg.Trim().Split('=')[1];
            }
            else if (arg.ToLower().StartsWith("-localport="))
            {
                systemConfig.LocalPort = Convert.ToInt32(arg.Trim().Split('=')[1]);
            }        
            else if (arg.ToLower().StartsWith("-maxlogdays="))
            {
                systemConfig.MaxLogDays = Convert.ToInt32(arg.Trim().Split('=')[1]);
            }
            else if (arg.ToLower().StartsWith("-maxqueueport="))
            {
                systemConfig.MaxQueuePort = Convert.ToInt32(arg.Trim().Split('=')[1]);
            }
            else if (arg.ToLower().StartsWith("-minqueueport="))
            {
                systemConfig.MinQueuePort = Convert.ToInt32(arg.Trim().Split('=')[1]);
            }        
        }

        if (systemConfig.LocalPort <= 0)
        {
            throw new ArgumentException($"Local Port config setting is invalid");
        }
        if (systemConfig.MaxLogDays < 0)
        {
            throw new ArgumentException($"Max Log Days config setting is invalid");
        }
        if (systemConfig.MaxQueuePort < 0)
        {
            throw new ArgumentException($"Max Queue Port config setting is invalid");
        }
        if (systemConfig.MinQueuePort < 0)
        {
            throw new ArgumentException($"Min Queue Port config setting is invalid");
        }     
        if (String.IsNullOrEmpty(systemConfig.AdminSecurityKey))
        {
            throw new ArgumentException($"Admin Security Key config setting is invalid");
        }

        return systemConfig;
    }
   
    private static IServiceProvider CreateServiceProvider(SystemConfig systemConfig)
    {
        var configFolder = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "Config");
        var logFolder = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "Log");

        var connectionString = $"Data Source={Path.Combine(configFolder, "CFMessageQueue.db")}";
        //var connectionString = "Data Source=D:\\Data\\Dev\\C#\\cf-message-queue-local\\CFMessageQueue.db";

        var configuration = new ConfigurationBuilder()                       
            .Build();

        var serviceProvider = new ServiceCollection()
              // Add data services
              .AddScoped<IMessageHubClientService, EFMessageHubClientService>()
              .AddScoped<IMessageQueueService, EFMessageQueueService>()
              .AddScoped<IQueueMessageHubService, EFQueueMessageHubService>()
              .AddScoped<IQueueMessageInternalService, EFQueueMessageInternalService>()

              .RegisterAllTypes<IMessageProcessor>(new[] { typeof(Program).Assembly })

              /*
              .AddScoped<IMessageHubClientService>((scope) =>
              {
                  return new XmlMessageHubClientService(Path.Combine(configFolder, "MessageHubClient"));
              })
             .AddScoped<IMessageQueueService>((scope) =>
             {
                 return new XmlMessageQueueService(Path.Combine(configFolder, "MessageQueue"));
             })
             .AddScoped<IQueueMessageHubService>((scope) =>
             {
                 return new XmlQueueMessageHubService(Path.Combine(configFolder, "QueueMessageHub"));
             })
             .AddScoped<IQueueMessageInternalService>((scope) =>
             {
                 return new XmlQueueMessageInternalService(Path.Combine(configFolder, "QueueMessageInternal"));
             }) 
             */

              // Add logging (Console & CSV)
              .AddScoped<ISimpleLog>((scope) =>
              {
                  return new SimpleMultiLog(new() {
                        new SimpleConsoleLog(),
                        new SimpleLogCSV(Path.Combine(logFolder, "MessageQueueHub-Simple-{date}.txt"))
                    });
              })

              .AddScoped<IAuditLog>((scope) =>
              {
                  return new AuditLogCSV(Path.Combine(logFolder, "MessageQueueHub-Audit-{date}.txt"));
              })

              // Add SQLite EF Core
              //.AddDbContext<CFMessageQueueDataContext>(options => options.UseSqlite(connectionString), ServiceLifetime.Scoped)
              .AddDbContextFactory<CFMessageQueueDataContext>(options => options.UseSqlite(connectionString), ServiceLifetime.Scoped)

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