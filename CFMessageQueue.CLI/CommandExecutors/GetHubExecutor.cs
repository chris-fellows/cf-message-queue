using CFCommandInterpreter.Models;
using CFMessageQueue.CLI.Interfaces;
using CFMessageQueue.CLI.Models;

namespace CFMessageQueue.CLI.CommandExecutors
{
    internal class GetHubExecutor : ICommandExecutor
    {
        private readonly IConnectionService _connectionData;

        public GetHubExecutor(IConnectionService connectionData)
        {
            _connectionData = connectionData;
        }

        public List<string> CommandFormats
        {
            get
            {
                return new() { "get-hub" };
            }
        }

        public Task<CommandResult> ExecuteAsync(Command command)
        {
            return Task.Factory.StartNew(() =>
            {
                return new CommandResult()
                {
                    Output = new List<string>() { $"IP: {_connectionData.RemoteEndpointInfo.Ip} Port: {_connectionData.RemoteEndpointInfo.Port}" }
                };
            });
        }

        public bool Supports(Command command)
        {
            return command.Name.Equals("get-hub", StringComparison.InvariantCultureIgnoreCase);
        }

        public string Validate(Command command)
        {           
            return String.Empty;
        }
    }
}
