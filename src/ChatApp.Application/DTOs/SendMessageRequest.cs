using ChatApp.Domain.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatApp.Application.DTOs
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
