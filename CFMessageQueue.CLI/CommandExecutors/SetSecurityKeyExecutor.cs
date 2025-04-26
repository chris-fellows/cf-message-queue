using CFCommandInterpreter.Models;
using CFMessageQueue.Interfaces;
using CFMessageQueue.CLI.Interfaces;
using CFMessageQueue.CLI.Models;

namespace CFMessageQueue.CLI.CommandExecutors
{
    internal class SetSecurityKeyExecutor : ICommandExecutor
    {        
        private readonly IConnectionService _connectionData;

        public SetSecurityKeyExecutor(IConnectionService connectionData)
        {
            _connectionData = connectionData;
        }

        public List<string> CommandFormats
        {
            get
            {
                return new() { "set-security-key -key [Key]" };
            }
        }

        public Task<CommandResult> ExecuteAsync(Command command)
        {
            return Task.Run(() =>
            {
                try
                {
                    _connectionData.SecurityKey = command.Switches.First(s => s.Name.Equals("-key", StringComparison.InvariantCultureIgnoreCase)).Value;

                    return new CommandResult()
                    {
                        Output = new List<string>() { "Security key set" }
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
            return command.Name.Equals("set-security-key", StringComparison.InvariantCultureIgnoreCase);
        }

        public string Validate(Command command)
        {               
            return String.Empty;
        }
    }
}
