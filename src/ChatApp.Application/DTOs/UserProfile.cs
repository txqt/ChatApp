using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatApp.Application.DTOs
{
    public class UserProfile
    {
        public string Sub { get; set; } = null!; // Auth0 user_id
        public string Email { get; set; } = null!;
        public string Name { get; set; } = null!;
    }
}
