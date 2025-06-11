using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatApp.Domain.Entities
{
    public class ApplicationUser : IdentityUser<string>
    {
        public string DisplayName { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastSeenAt { get; set; }
        public bool IsOnline { get; set; } = false;

        // Navigation properties
        public UserPermission? UserPermission { get; set; }
        public ICollection<ApplicationUserRole> UserRoles { get; set; } = new List<ApplicationUserRole>();
        public ICollection<Message> Messages { get; set; } = new List<Message>();
        public ICollection<ChatMember> ChatMemberships { get; set; } = new List<ChatMember>();
        public ICollection<MediaFile> UploadedFiles { get; set; } = new List<MediaFile>();
        public ICollection<Chat> CreatedChats { get; set; } = new List<Chat>();
        public virtual ICollection<Friendship> SentFriendRequests { get; set; } = new List<Friendship>();
        public virtual ICollection<Friendship> ReceivedFriendRequests { get; set; } = new List<Friendship>();
    }
}
