using CFMessageQueue.CLI.Models;

namespace CFMessageQueue.CLI.Interfaces
{
    internal interface IProcessorService
    {
        CommandResult? Process(string input);
    }
}
