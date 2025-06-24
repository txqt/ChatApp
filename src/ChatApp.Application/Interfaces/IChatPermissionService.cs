using ChatApp.Domain.Entities;
using ChatApp.Domain.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatApp.Application.Interfaces
{
    public interface IChatPermissionService
    {
        Task<bool> CanUserPerformAction(string userId, int chatId, ChatPermissions permission);
        Task<bool> CanUserPerformAction(ApplicationUser user, int chatId, ChatPermissions permission);
        Task<bool> UpdateRolePermissions(int chatId, ChatMemberRole role, ChatPermissions permissions, string updatedBy);
        Task<ChatPermissions> GetUserPermissions(string userId, int chatId);
        Task<ChatPermissions> GetUserPermissions(ApplicationUser user, int chatId);
        Task<ChatPermissions> GetRolePermissions(int chatId, ChatMemberRole role);
    }

}
