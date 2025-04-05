using System.ComponentModel;

namespace CFMessageQueue.Enums
{
    public enum ResponseErrorCodes
    {
        [Description("Invalid parameters")]
        InvalidParameters,

        [Description("Message queue does not exist")]
        MessageQueueDoesNotExist,

        [Description("Message queue is full")]
        MessageQueueFull,

        [Description("Unknown")]
        Unknown,

        [Description("Permission denied")]
        PermissionDenied
    }
}
