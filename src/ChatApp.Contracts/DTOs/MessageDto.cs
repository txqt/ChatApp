using ChatApp.Domain.Enum;

namespace ChatApp.Contracts.DTOs
{
    public class MessageDto
    {
        public int MessageId { get; set; }
        public int ChatId { get; set; }
        public string Content { get; set; } = string.Empty;
        public MessageType MessageType { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsEdited { get; set; }
        public bool IsDeleted { get; set; }
        public UserDto Sender { get; set; } = new();
        public List<MediaFileModel>? MediaFiles { get; set; }
        public MessageDto? ReplyTo { get; set; }
        public bool IsForwarded { get; set; }
        public bool IsFromCurrentUser { get; set; }
    }
}
