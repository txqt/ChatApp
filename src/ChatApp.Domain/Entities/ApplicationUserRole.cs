using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatApp.Domain.Entities
{
    public class ApplicationUserRole : IdentityUserRole<string>
    {
        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
        public string? AssignedBy { get; set; }

        // Navigation properties
        public ApplicationUser User { get; set; } = null!;
        public ApplicationRole Role { get; set; } = null!;
        public ApplicationUser? AssignedByUser { get; set; }
    }
}
