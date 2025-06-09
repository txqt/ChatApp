using ChatApp.Domain.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatApp.Application.DTOs
{
    public class FriendRequestDto
    {
        public string Id { get; set; } = string.Empty;
        public int RequesterId { get; set; }
        public string RequesterName { get; set; } = string.Empty;
        public string RequesterDisplayName { get; set; } = string.Empty;
        public string RequesterAvatar { get; set; } = string.Empty;
        public int ReceiverId { get; set; }
        public FriendshipStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
