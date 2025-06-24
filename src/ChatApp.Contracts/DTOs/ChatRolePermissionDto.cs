using ChatApp.Domain.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatApp.Contracts.DTOs
{
    public class ChatRolePermissionDto
    {
        public ChatMemberRole Role { get; set; }
        public ChatPermissions Permissions{ get; set; }
    }
}
