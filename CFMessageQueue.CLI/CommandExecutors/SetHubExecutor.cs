using CFCommandInterpreter.Models;
using CFConnectionMessaging.Models;
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
    internal class SetHubExecutor : ICommandExecutor
    {
        private readonly IConnectionService _connectionData;

        public SetHubExecutor(IConnectionService connectionData)
        {
            _connectionData = connectionData;
        }

        public List<string> CommandFormats
        {
            get
            {
                return new() { "set-hub -ip [IP] -port [Port]" };
            }
        }

        public Task<CommandResult> ExecuteAsync(Command command)
        {
            return Task.Run(() =>
            {
                try
                {
                    _connectionData.RemoteEndpointInfo = new EndpointInfo()
                    {
                        Ip = command.Switches.First(s => s.Name.Equals("-ip", StringComparison.InvariantCultureIgnoreCase)).Value,
                        Port = Convert.ToInt32(command.Switches.First(s => s.Name.Equals("-port", StringComparison.InvariantCultureIgnoreCase)).Value)
                    };

                    _connectionData.MessageHubClientConnector = new MessageHubClientConnector(_connectionData.RemoteEndpointInfo, _connectionData.SecurityKey, 10200);

                    return new CommandResult()
                    {
                        Output = new List<string>() { "Hub set" }
                    };
                }
                catch (Exception exception)
                {
                    return new CommandResult() { Output = new List<string>() { $"Error: {exception.Message}" } };
                }
            });
        }

        public bool Supports(Command command)
        {
            return command.Name.Equals("set-hub", StringComparison.InvariantCultureIgnoreCase);
        }

        public string Validate(Command command)
        {
            if (String.IsNullOrEmpty(_connectionData.SecurityKey))
            {
                return "Error: You must call set-security-key to set the security key";
            }

            return String.Empty;
        }
    }
}
