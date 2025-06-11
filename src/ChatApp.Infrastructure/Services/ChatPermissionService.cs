using ChatApp.Application.Interfaces;
using ChatApp.Domain.Entities;
using ChatApp.Domain.Enum;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace ChatApp.Infrastructure.Services
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

    public class ChatPermissionService : IChatPermissionService
    {
        private readonly IApplicationDbContext _context;

        public ChatPermissionService(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> CanUserPerformAction(string userId, int chatId, ChatPermissions permission)
        {
            var userPermissions = await GetUserPermissions(userId, chatId);
            return userPermissions.HasFlag(permission);
        }

        public async Task<bool> CanUserPerformAction(ApplicationUser user, int chatId, ChatPermissions permission)
        {
            var userPermissions = await GetUserPermissions(user, chatId);
            return userPermissions.HasFlag(permission);
        }

        public async Task<ChatPermissions> GetUserPermissions(string userId, int chatId)
        {
            var member = await _context.ChatMembers
                .FirstOrDefaultAsync(cm => cm.UserId == userId && cm.ChatId == chatId && cm.IsActive);

            if (member == null)
                return ChatPermissions.None;

            var chat = await _context.Chats
                .Include(c => c.RolePermissions)
                .FirstOrDefaultAsync(c => c.ChatId == chatId);

            if (chat == null)
                return ChatPermissions.None;

            return chat.GetRolePermissions(member.Role);
        }

        public async Task<ChatPermissions> GetUserPermissions(ApplicationUser user, int chatId)
        {
            var member = await _context.ChatMembers
                .FirstOrDefaultAsync(cm => cm.UserId == user.Id && cm.ChatId == chatId && cm.IsActive);

            if (member == null)
                return ChatPermissions.None;

            var chat = await _context.Chats
                .Include(c => c.RolePermissions)
                .FirstOrDefaultAsync(c => c.ChatId == chatId);

            if (chat == null)
                return ChatPermissions.None;

            return chat.GetRolePermissions(member.Role);
        }

        public async Task<ChatPermissions> GetRolePermissions(int chatId, ChatMemberRole role)
        {
            var chat = await _context.Chats
                .Include(c => c.RolePermissions)
                .FirstOrDefaultAsync(c => c.ChatId == chatId);

            return chat?.GetRolePermissions(role) ?? ChatPermissions.None;
        }

        public async Task<bool> UpdateRolePermissions(int chatId, ChatMemberRole role, ChatPermissions permissions, string updatedBy)
        {
            // Kiểm tra quyền của người cập nhật
            var canManage = await CanUserPerformAction(updatedBy, chatId, ChatPermissions.ManagePermissions);
            if (!canManage)
                return false;

            var existingPermission = await _context.ChatRolePermissions
                .FirstOrDefaultAsync(rp => rp.ChatId == chatId && rp.Role == role);

            if (existingPermission != null)
            {
                existingPermission.PermissionMask = (long)permissions;
                existingPermission.UpdatedAt = DateTime.UtcNow;
                existingPermission.UpdatedBy = updatedBy;
            }
            else
            {
                _context.ChatRolePermissions.Add(new ChatRolePermission
                {
                    ChatId = chatId,
                    Role = role,
                    PermissionMask = (long)permissions,
                    UpdatedBy = updatedBy
                });
            }

            await _context.SaveChangesAsync();
            return true;
        }
    }
}