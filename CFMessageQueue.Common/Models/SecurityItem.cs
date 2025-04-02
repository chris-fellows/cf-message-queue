using CFMessageQueue.Enums;

namespace CFMessageQueue.Models
{
    public class SecurityItem
    {
        public string MessageHubClientId { get; set; } = String.Empty;

        public List<RoleTypes> RoleTypes { get; set; } = new();
    }
}
