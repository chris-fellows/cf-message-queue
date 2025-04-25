using System.Reflection;
using CFMessageQueue.CLI.Interfaces;
using CFMessageQueue.CLI.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
internal static class Program
{
    private static void Main(string[] args)
    {
        Console.WriteLine($"Starting CF Message Queue CLI");

        // Get service provider
        var serviceProvider = CreateServiceProvider();

        var processorService = serviceProvider.GetRequiredService<IProcessorService>();

        // Process commands until user types exit
        Console.WriteLine("Type exit to quit");
        var exit = false;
        do
        {
            Console.Write("Command:>");
            var input = Console.ReadLine();

            var commandResult = processorService.Process(input);

            exit = commandResult == null ? false : commandResult.Exit;
        } while (!exit);


        Console.WriteLine("Terminated Starting CF Message Queue CLI");
    }

    private static IServiceProvider CreateServiceProvider()
    {        
        var configuration = new ConfigurationBuilder()
            .Build();

        var serviceProvider = new ServiceCollection()
             .AddScoped<IProcessorService, ProcessorService>()
             .AddSingleton<IConnectionService, ConnectionService>()
             .RegisterAllTypes<ICommandExecutor>(new[] { typeof(Program).Assembly })
              
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