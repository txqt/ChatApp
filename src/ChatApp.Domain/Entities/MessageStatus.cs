using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatApp.Domain.Entities
{
    public class MessageStatus
    {
        public int MessageId { get; set; }
        public string UserId { get; set; }
        public MessageStatusType Status { get; set; } = MessageStatusType.Sent;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public Message Message { get; set; } = null!;
        public ApplicationUser User { get; set; } = null!;
    }
}
