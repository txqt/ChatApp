using ChatApp.Domain.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatApp.Domain.Entities
{
    public class Chat
    {
        public int ChatId { get; set; }
        public ChatType ChatType { get; set; }
        public string? ChatName { get; set; } // Null for direct chats
        public string? Description { get; set; }
        public string? AvatarUrl { get; set; }
        public int CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
        public int? LastMessageId { get; set; }

        // Group chat settings
        public bool AllowMembersToAddOthers { get; set; } = true;
        public bool AllowMembersToEditInfo { get; set; } = false;
        public int MaxMembers { get; set; } = 1000;

        // Navigation properties
        public ApplicationUser Creator { get; set; } = null!;
        public Message? LastMessage { get; set; }
        public ICollection<ChatMember> Members { get; set; } = new List<ChatMember>();
        public ICollection<Message> Messages { get; set; } = new List<Message>();
    }
}
