using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatApp.Application.DTOs
{
    public class FriendDto
    {
        public string Id { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Avatar { get; set; } = string.Empty;
        public bool IsOnline { get; set; }
        public DateTime? LastSeen { get; set; }
        public DateTime FriendsSince { get; set; }
    }
}
