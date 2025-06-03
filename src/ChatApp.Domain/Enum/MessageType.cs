using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatApp.Domain.Enum
{
    public enum MessageType
    {
        Text = 0,
        Image = 1,
        File = 2,
        Audio = 3,
        Video = 4,
        System = 5, // System messages (user joined, left, etc.)
        Sticker = 6,
        Location = 7
    }
}
