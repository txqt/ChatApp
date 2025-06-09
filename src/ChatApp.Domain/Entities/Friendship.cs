using ChatApp.Domain.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatApp.Domain.Entities
{
    public class Friendship
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public int RequesterId { get; set; }
        public int ReceiverId { get; set; }
        public FriendshipStatus Status { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? AcceptedAt { get; set; }

        // Navigation properties
        public virtual ApplicationUser Requester { get; set; } = null!;
        public virtual ApplicationUser Receiver { get; set; } = null!;
    }
}
