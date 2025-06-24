using ChatApp.Domain.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatApp.Contracts.DTOs
{
    public class UserDto
    {
        public string Id { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public bool IsOnline { get; set; }
        public ChatMemberRole Role { get; set; }
        public DateTime? LastSeenAt { get; set; }
    }
}
