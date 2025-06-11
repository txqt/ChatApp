using ChatApp.Domain.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatApp.Domain.Entities
{
    public class UserPermission
    {
        public string UserId { get; set; }
        public long PermissionMask { get; set; } = (long)AppPermissions.BasicUser;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public string? UpdatedBy { get; set; }

        // Navigation properties
        public ApplicationUser User { get; set; } = null!;
        public ApplicationUser? UpdatedByUser { get; set; }
    }
}
