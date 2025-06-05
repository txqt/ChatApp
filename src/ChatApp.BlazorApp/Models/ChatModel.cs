using ChatApp.Domain.Enum;

namespace ChatApp.BlazorApp.Models
{
    public class ChatModel
    {
        public int ChatId { get; set; }
        public string? ChatName { get; set; }
        public ChatType ChatType { get; set; }
        public string? AvatarUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public MessageModel? LastMessage { get; set; }
        public List<UserModel> Members { get; set; } = new();
        public int UnreadCount { get; set; }
        public DateTime? LastReadAt { get; set; }
        public bool IsMuted { get; set; }
    }
}