﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatApp.Contracts.DTOs
{
    public class EditMessageRequest
    {
        public string Content { get; set; } = string.Empty;
    }
}
