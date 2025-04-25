using CFCommandInterpreter.Models;
using CFMessageQueue.CLI.Interfaces;
using CFMessageQueue.CLI.Models;

namespace CFMessageQueue.CLI.CommandExecutors
{
    internal class GetMessageHubClientsExecutor
    {
        private readonly IConnectionService _connectionData;

        public GetMessageHubClientsExecutor(IConnectionService connectionData)
        {
            _connectionData = connectionData;
        }

        public List<string> CommandFormats
        {
            get
            {
                return new() { "get-hub-clients" };
            }
        }

        public Task<CommandResult> ExecuteAsync(Command command)
        {
            return Task.Factory.StartNew(() =>
            {
                try
                {
                    var result = _connectionData.MessageHubClientConnector.GetMessageHubClientsAsync().Result;

                    return new CommandResult()
                    {
                        Output = result.Any() ? result.Select(c => $"ID: {c.Id}; Name:{c.Name}").ToList() : new List<string>() { "No clients" }
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
            return command.Name.Equals("get-hub-clients", StringComparison.InvariantCultureIgnoreCase);
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
