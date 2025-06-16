using ChatApp.Domain.Enum;
using Microsoft.AspNetCore.Http;
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
        public string? Content { get; set; }
        public MessageType MessageType { get; set; } = MessageType.Text;
        public int? ReplyToMessageId { get; set; }
        public IFormFile? File { get; set; }
    }

}
