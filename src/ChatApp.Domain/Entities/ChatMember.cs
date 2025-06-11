using ChatApp.Domain.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatApp.Domain.Entities
{
    public class ChatMember
    {
        public int ChatId { get; set; }
        public string UserId { get; set; }
        public ChatMemberRole Role { get; set; } = ChatMemberRole.Member;
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
        public string? AddedBy { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime? LeftAt { get; set; }
        public bool IsMuted { get; set; } = false;
        public DateTime? MutedUntil { get; set; }

        // Read status
        public int? LastReadMessageId { get; set; }
        public DateTime? LastReadAt { get; set; }

        // Navigation properties
        public Chat Chat { get; set; } = null!;
        public ApplicationUser User { get; set; } = null!;
        public ApplicationUser? AddedByUser { get; set; }
        public Message? LastReadMessage { get; set; }
    }
}
