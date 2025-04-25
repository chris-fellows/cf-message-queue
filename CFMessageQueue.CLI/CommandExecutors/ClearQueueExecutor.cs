using CFCommandInterpreter.Models;
using CFMessageQueue.CLI.Interfaces;
using CFMessageQueue.CLI.Models;
using CFMessageQueue.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CFMessageQueue.CLI.CommandExecutors
{
    internal class ClearQueueExecutor : ICommandExecutor
    {
        private readonly IConnectionService _connectionData;

        public ClearQueueExecutor(IConnectionService connectionData)
        {
            _connectionData = connectionData;
        }

        public List<string> CommandFormats
        {
            get
            {
                return new() { "clear-queue -name [Queue Name]" };
            }
        }

        public Task<CommandResult> ExecuteAsync(Command command)
        {
            return Task.Factory.StartNew(() =>
            {
                var queueName = command.Switches.First(s => s.Name.Equals("-name", StringComparison.InvariantCultureIgnoreCase)).Value;

                try
                {
                    var result = _connectionData.MessageHubClientConnector.ClearMessageQueueAsync(queueName).Result;

                    return new CommandResult() { Output = new List<string>() { "Queue cleared" } };
                }
                catch(Exception exception)
                {
                    return new CommandResult() { Output = new List<string>() { $"Error: {exception.Message}" } };
                }
            });
        }

        public bool Supports(Command command)
        {
            return command.Name.Equals("clear-queue", StringComparison.InvariantCultureIgnoreCase);
        }
        
        public string Validate(Command command)
        {
            if (String.IsNullOrEmpty(_connectionData.RemoteEndpointInfo.Ip))
            {
                return "Error: You must call set-hub to set the hub location";
            }
            if (String.IsNullOrEmpty(_connectionData.SecurityKey))
            {
                return "Error: You must call set-security-key to set the security key";
            }

            return String.Empty;
        }
    }
}
