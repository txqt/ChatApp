using ChatApp.Domain.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatApp.Application.DTOs
{
    public class UserSearchDto
    {
        public string Id { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Avatar { get; set; } = string.Empty;
        public bool IsOnline { get; set; }
        public DateTime? LastSeen { get; set; }
        public FriendshipStatus? FriendshipStatus { get; set; }
        public bool IsFriend { get; set; }
        public bool HasPendingRequest { get; set; }
    }
}
