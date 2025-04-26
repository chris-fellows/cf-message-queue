using CFCommandInterpreter.Models;
using CFConnectionMessaging.Models;
using CFMessageQueue.CLI.Interfaces;
using CFMessageQueue.CLI.Models;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace CFMessageQueue.CLI.CommandExecutors
{
    internal class HelpExecutor : ICommandExecutor
    {
        private readonly IConnectionService _connectionData;
        private readonly IServiceProvider _serviceProvider;

        public HelpExecutor(IConnectionService connectionData,
                            IServiceProvider serviceProvider)   
        {
            _connectionData = connectionData;
            _serviceProvider = serviceProvider;
        }

        public List<string> CommandFormats
        {
            get
            {
                return new() 
                { 
                    "help",
                    "help -command [CommandName]"                
                };
            }
        }

        public Task<CommandResult> ExecuteAsync(Command command)
        {
            return Task.Run(() =>
            {
                var commandExecutors = _serviceProvider.GetServices<ICommandExecutor>();

                var results = new List<string>();
                results.Add("Commands:");

                foreach(var commandExecutor in commandExecutors.OrderBy(e => e.CommandFormats.First()))
                {
                    foreach(var format in commandExecutor.CommandFormats)
                    {
                        results.Add(format);
                    }
                }

                return new CommandResult()
                {
                    Output = results
                };
            });
        }

        public bool Supports(Command command)
        {
            return command.Name.Equals("help", StringComparison.InvariantCultureIgnoreCase);
        }

        public string Validate(Command command)
        {           
            return String.Empty;
        }
    }
}
