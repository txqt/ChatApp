using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatApp.Contracts.DTOs
{
    public class UserInfoDto
    {
        public string Id { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public string Picture { get; set; }
        public string Role { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
