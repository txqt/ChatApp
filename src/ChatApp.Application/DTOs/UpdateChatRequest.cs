﻿using ChatApp.Domain.Enum;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ChatApp.Application.DTOs
{
    public class UpdateChatRequest
    {
        public int ChatId { get; set; }
        public string? ChatName { get; set; }
        public string? Description { get; set; }
        public bool AllowMembersToAddOthers { get; set; }
        public bool AllowMembersToEditInfo { get; set; }
        public int? MaxMembers { get; set; } = 1000;
        public List<ChatRolePermissionDto>? RolePermissions { get; set; }
        public IFormFile Avatar { get; set; } = null!;
    }
}
