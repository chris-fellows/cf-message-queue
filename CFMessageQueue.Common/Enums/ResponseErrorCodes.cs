using System.ComponentModel;

namespace CFMessageQueue.Enums
{
    public enum ResponseErrorCodes
    {
        [Description("Invalid parameters")]
        InvalidParameters,
        [Description("Unknown")]
        Unknown,
        [Description("Permission denied")]
        PermissionDenied
    }
}
