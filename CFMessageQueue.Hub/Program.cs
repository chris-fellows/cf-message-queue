using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Runtime.Loader;
using CFMessageQueue.Hub.Models;
using CFMessageQueue.Hub;
using CFMessageQueue.Interfaces;

internal static class Program
{
    private static void Main(string[] args)
    {
        var hostEntry = Dns.GetHostEntry(Dns.GetHostName());
        Console.WriteLine($"Starting CF Message Queue Hub ({hostEntry.AddressList[0].ToString()})");

        var serviceProvider = CreateServiceProvider();

        var messageQueueService = serviceProvider.GetRequiredService<IMessageQueueService>();

        // Create message queue workers, one per queue
        var messageQueueWorkers = new List<MessageQueueWorker>();
        var messageQueues = messageQueueService.GetAllAsync().Result;
        foreach(var messageQueue in messageQueues)
        {
            messageQueueWorkers.Add(new MessageQueueWorker(messageQueue, serviceProvider));
        }

        // Start workers
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

            // Stop worker            
            messageQueueWorkers.ForEach(worker => worker.Stop());
        }

        Console.WriteLine("Terminated Starting CF Message Queue Hub");
    }

    private static void Default_Unloading(AssemblyLoadContext obj)
    {
        throw new NotImplementedException();
    }

    private static bool IsInDockerContainer
    {
        get { return Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true"; }
    }

    private static IServiceProvider CreateServiceProvider()
    {
        var configFolder = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "Config");
        var logFolder = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "Log");
        
        // TODO: Register dependencies
        var configuration = new ConfigurationBuilder()           

            .Build();

        var serviceProvider = new ServiceCollection()                
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