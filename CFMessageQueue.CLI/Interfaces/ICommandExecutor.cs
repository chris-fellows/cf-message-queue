using CFCommandInterpreter.Models;
using CFMessageQueue.CLI.Models;

namespace CFMessageQueue.CLI.Interfaces
{
    /// <summary>
    /// Execute command
    /// </summary>
    internal interface ICommandExecutor
    {
        /// <summary>
        /// Command formats supported. Can be displayed in UI
        /// </summary>
        List<string> CommandFormats { get; }

        /// <summary>
        /// Executes command
        /// </summary>
        /// <returns></returns>
        Task<CommandResult> ExecuteAsync(Command command);

        /// <summary>
        /// Whether instance supports command name. Doesn't check if switches are valid.
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        bool Supports(Command command);

        /// <summary>
        /// Validates command prior to execute. Checks that parameters appear valid.
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        string Validate(Command command);
    }
}
