using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatApp.Domain.Entities
{
    public class ApplicationRole : IdentityRole<string>
    {
        public string Description { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public RolePermission? RolePermission { get; set; }
        public ICollection<ApplicationUserRole> UserRoles { get; set; } = new List<ApplicationUserRole>();
    }
}
