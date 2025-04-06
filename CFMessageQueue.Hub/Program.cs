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

internal static class Program
{
    private static void Main(string[] args)
    {
        Console.WriteLine($"Starting CF Message Queue Hub ({NetworkUtilities.GetLocalIPV4Addresses()[0]})");

        // Get system config
        var systemConfig = GetSystemConfig();

        // Get service provider
        var serviceProvider = CreateServiceProvider();

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

    private static SystemConfig GetSystemConfig()
    {            
        return new SystemConfig()
        {
            LocalPort = Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["LocalPort"].ToString()),
            MinQueuePort = Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["MinQueuePort"].ToString()),
            MaxQueuePort = Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["MaxQueuePort"].ToString()),
            MaxLogDays = Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["MaxLogDays"].ToString()),
            LogFolder = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "Log"),
            AdminSecurityKey = System.Configuration.ConfigurationManager.AppSettings["AdminSecurityKey"].ToString()
        };
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
             .AddScoped<IQueueMessageInternalService>((scope) =>
             {
                 return new XmlQueueMessageInternalService(Path.Combine(configFolder, "QueueMessageInternal"));
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
                        new SimpleLogCSV(Path.Combine(logFolder, "MessageQueueHub-Simple-{date}.txt"))
                    });
              })

              .AddScoped<IAuditLog>((scope) =>
              {
                  return new AuditLogCSV(Path.Combine(logFolder, "MessageQueueHub-Audit-{date}.txt"));
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