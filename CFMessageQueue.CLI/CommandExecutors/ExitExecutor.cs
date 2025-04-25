using CFCommandInterpreter.Models;
using CFMessageQueue.CLI.Interfaces;
using CFMessageQueue.CLI.Models;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CFMessageQueue.CLI.CommandExecutors
{
    internal class ExitExecutor
    {
        private readonly IConnectionService _connectionData;
        private readonly IServiceProvider _serviceProvider;

        public ExitExecutor(IConnectionService connectionData,
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
                    "exit",                    
                };
            }
        }

        public Task<CommandResult> ExecuteAsync(Command command)
        {
            return Task.Factory.StartNew(() =>
            {
                return new CommandResult() { Exit = true };   
            });
        }

        public bool Supports(Command command)
        {
            return command.Name.Equals("exit", StringComparison.InvariantCultureIgnoreCase);
        }

        public string Validate(Command command)
        {
            return String.Empty;
        }
    }
}
