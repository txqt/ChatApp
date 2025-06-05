using ChatApp.Domain.Enum;

namespace ChatApp.BlazorApp.Models
{
    public class SendMessageRequest
    {
        public int ChatId { get; set; }
        public string Content { get; set; } = string.Empty;
        public MessageType MessageType { get; set; } = MessageType.Text;
        public int? MediaFileId { get; set; }
        public int? ReplyToMessageId { get; set; }
    }
}