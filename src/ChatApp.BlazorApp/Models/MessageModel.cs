using ChatApp.Domain.Enum;

namespace ChatApp.BlazorApp.Models
{
    public class MessageModel
    {
        public int MessageId { get; set; }
        public int ChatId { get; set; }
        public string Content { get; set; } = string.Empty;
        public MessageType MessageType { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsEdited { get; set; }
        public bool IsDeleted { get; set; }
        public UserModel Sender { get; set; } = new();
        public MediaFileModel? MediaFile { get; set; }
        public MessageModel? ReplyTo { get; set; }
        public bool IsForwarded { get; set; }
        public bool IsFromCurrentUser { get; set; }
    }
}