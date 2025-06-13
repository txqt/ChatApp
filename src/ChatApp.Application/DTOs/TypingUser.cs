using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatApp.Application.DTOs
{
    public class TypingUser
    {
        public string UserId { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
    }
}
