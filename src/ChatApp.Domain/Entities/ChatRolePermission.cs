using ChatApp.Domain.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatApp.Domain.Entities
{
    public class ChatRolePermission
    {
        public int ChatId { get; set; }
        public ChatMemberRole Role { get; set; }
        public long PermissionMask { get; set; } = (long)ChatPermissions.All;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public string UpdatedBy { get; set; }

        // Navigation properties
        public Chat Chat { get; set; } = null!;
        public ApplicationUser UpdatedByUser { get; set; } = null!;
    }
}
