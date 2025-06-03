using ChatApp.Domain.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatApp.Domain.Entities
{
    public class RolePermission
    {
        public int RoleId { get; set; }
        public long PermissionMask { get; set; } = (long)AppPermissions.BasicUser;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public int? UpdatedBy { get; set; }

        // Navigation properties
        public ApplicationRole Role { get; set; } = null!;
        public ApplicationUser? UpdatedByUser { get; set; }
    }
}
