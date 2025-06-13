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
        public string RequesterId { get; set; }
        public string ReceiverId { get; set; }
        public FriendshipStatus Status { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? AcceptedAt { get; set; }

        // Navigation properties
        public ApplicationUser? Requester { get; set; }
        public ApplicationUser? Receiver { get; set; }
    }
}
