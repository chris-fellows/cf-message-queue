using CFMessageQueue.Enums;
using System.ComponentModel.DataAnnotations;

namespace CFMessageQueue.Models
{
    public class SecurityItem
    {
        [MaxLength(50)]
        public string Id { get; set; } = String.Empty;

        [MaxLength(50)]
        public string MessageHubClientId { get; set; } = String.Empty;

        public List<RoleTypes> RoleTypes { get; set; } = new();
    }
}
