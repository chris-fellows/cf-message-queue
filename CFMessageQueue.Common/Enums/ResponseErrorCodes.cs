using System.ComponentModel;

namespace CFMessageQueue.Enums
{
    public enum ResponseErrorCodes
    {
        [Description("Unknown")]
        Unknown,
        [Description("Permission denied")]
        PermissionDenied
    }
}
