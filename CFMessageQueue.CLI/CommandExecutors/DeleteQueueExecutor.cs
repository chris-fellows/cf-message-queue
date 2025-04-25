using CFCommandInterpreter.Models;
using CFMessageQueue.CLI.Interfaces;
using CFMessageQueue.CLI.Models;

namespace CFMessageQueue.CLI.CommandExecutors
{
    internal class DeleteQueueExecutor : ICommandExecutor
    {
        private readonly IConnectionService _connectionData;

        public DeleteQueueExecutor(IConnectionService connectionData)
        {
            _connectionData = connectionData;
        }

        public List<string> CommandFormats
        {
            get
            {
                return new() { "delete-queue -name [Queue Name]" };
            }
        }

        public Task<CommandResult> ExecuteAsync(Command command)
        {
            return Task.Factory.StartNew(() =>
            {
                try
                {
                    var queueName = command.Switches.First(s => s.Name.Equals("-name", StringComparison.InvariantCultureIgnoreCase)).Value;

                    //if (_connectionData.MessageHubClientConnector == null)
                    //{
                    //    _connectionData.MessageHubClientConnector = new MessageHubClientConnector(_connectionData.RemoteEndpointInfo, _connectionData.SecurityKey, 101200);
                    //}

                    var result = _connectionData.MessageHubClientConnector.ClearMessageQueueAsync(queueName).Result;

                    return new CommandResult() { Output = new List<string>() { "Queue deleted" } };
                }
                catch (Exception exception)
                {
                    return new CommandResult() { Output = new List<string>() { $"Error: {exception.Message}" } };
                }
            });
        }

        public bool Supports(Command command)
        {
            return command.Name.Equals("delete-queue", StringComparison.InvariantCultureIgnoreCase);
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
