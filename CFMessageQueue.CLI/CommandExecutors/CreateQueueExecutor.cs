using CFCommandInterpreter.Models;
using CFMessageQueue.CLI.Interfaces;
using CFMessageQueue.CLI.Models;
using CFMessageQueue.Services;
using Microsoft.EntityFrameworkCore.Storage.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CFMessageQueue.CLI.CommandExecutors
{
    internal class CreateQueueExecutor : ICommandExecutor
    {
        private readonly IConnectionService _connectionData;

        public CreateQueueExecutor(IConnectionService connectionData)
        {
            _connectionData = connectionData;
        }

        public List<string> CommandFormats
        {
            get
            {
                return new() { "create-queue -name [Queue Name] -max-concurrent-processing [Value] -max-size [Value]" };
            }
        }

        public Task<CommandResult> ExecuteAsync(Command command)
        {
            return Task.Run(() =>
            {
                try
                {
                    var queueName = command.Switches.First(s => s.Name.Equals("-name", StringComparison.InvariantCultureIgnoreCase)).Value;

                    var maxConcurrentProcessingSwitch = command.Switches.First(s => s.Name.Equals("-max-concurrent-processing", StringComparison.InvariantCultureIgnoreCase));
                    var maxConcurrentProcessing = maxConcurrentProcessingSwitch == null ? 0 : Convert.ToInt32(maxConcurrentProcessingSwitch.Value);

                    var maxSizeSwitch = command.Switches.First(s => s.Name.Equals("-max-size", StringComparison.InvariantCultureIgnoreCase));
                    var maxSize = maxSizeSwitch == null ? 0 : Convert.ToInt32(maxSizeSwitch.Value);

                    //if (_connectionData.MessageHubClientConnector == null)
                    //{
                    //    _connectionData.MessageHubClientConnector = new MessageHubClientConnector(_connectionData.RemoteEndpointInfo, _connectionData.SecurityKey, 101200);
                    //}

                    var result = _connectionData.MessageHubClientConnector.AddMessageQueueAsync(queueName, maxConcurrentProcessing, maxSize).Result;

                    return new CommandResult() { Output = new List<string>() { "Queue created" } };
                }
                catch (Exception exception)
                {
                    return new CommandResult() { Output = new List<string>() { $"Error: {exception.Message}" } };
                }
            });            
        }

        public bool Supports(Command command)
        {
            return command.Name.Equals("create-queue", StringComparison.InvariantCultureIgnoreCase);
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
