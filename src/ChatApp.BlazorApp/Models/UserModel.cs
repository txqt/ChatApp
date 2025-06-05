using ChatApp.Domain.Enum;

namespace ChatApp.BlazorApp.Models
{
    public class UserModel
    {
        public int Id { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public bool IsOnline { get; set; }
        public ChatMemberRole Role { get; set; }
        public DateTime? LastSeenAt { get; set; }
    }
}