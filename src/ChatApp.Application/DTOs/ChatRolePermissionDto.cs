using ChatApp.Domain.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatApp.Application.DTOs
{
    public class ChatRolePermissionDto
    {
        public ChatMemberRole Role { get; set; }
        public ChatPermissions Permissions{ get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public string UpdatedBy { get; set; }
    }
}
