using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatApp.Domain.Entities
{
    public enum MessageStatusType
    {
        Sent = 0,
        Delivered = 1,
        Read = 2,
        Failed = 3
    }
}
