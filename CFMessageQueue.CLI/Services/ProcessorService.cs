using CFCommandInterpreter.Common;
using CFCommandInterpreter.Models;
using CFMessageQueue.CLI.Interfaces;
using CFMessageQueue.CLI.Models;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CFMessageQueue.CLI.Services
{
    internal class ProcessorService : IProcessorService
    {
        private readonly IServiceProvider _servicerProvider;

        public ProcessorService(IServiceProvider serviceProvider)
        {
            _servicerProvider = serviceProvider;
        }

        public CommandResult? Process(string input)
        {
            using (var scope = _servicerProvider.CreateScope())
            {
                var commandInterpreter = new CommandInterpreter();
                commandInterpreter.Config = new InterpreterConfig()
                {
                    SwitchNameStartChar = '-',
                    SwitchNameEndChar = ' '
                };

                // Interpret command
                var command = commandInterpreter.Read(input);
                if (command != null)
                {
                    // Get command executor that processes command
                    var commandExecutor = scope.ServiceProvider.GetServices<ICommandExecutor>().FirstOrDefault(e => e.Supports(command));
                    if (commandExecutor == null)
                    {
                        Console.WriteLine("Error: Invalid command");
                    }
                    else
                    {
                        // Validate that command can be executed
                        var validateResult = commandExecutor.Validate(command);
                        if (String.IsNullOrEmpty(validateResult))
                        {
                            var commandResult = commandExecutor.ExecuteAsync(command).Result;

                            // Display results
                            foreach (var outputLine in commandResult.Output)
                            {
                                Console.WriteLine(outputLine);
                            }

                            return commandResult;
                        }
                        else
                        {
                            Console.WriteLine($"Error: Command invalid: {validateResult}");
                        }
                    }                    
                }

                return null;
            }
        }
    }
}
