using ChatApp.Domain.Entities;
using ChatApp.Domain.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatApp.Application.DTOs
{
    public class ChatDto
    {
        public int ChatId { get; set; }
        public string? ChatName { get; set; }
        public ChatType ChatType { get; set; }
        public string? AvatarUrl { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public MessageDto? LastMessage { get; set; }
        public List<UserDto> Members { get; set; } = new();
        public int UnreadCount { get; set; }
        public DateTime? LastReadAt { get; set; }
        public bool AllowMembersToAddOthers { get; set; } = true;
        public bool AllowMembersToEditInfo { get; set; } = false;
        public int MaxMembers { get; set; } = 1000;
        public ChatPermissions Permissions { get; set; }
        public List<ChatRolePermissionDto> ChatRolePermissions { get; set; }
    }
}
