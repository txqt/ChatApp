using ChatApp.Application.Interfaces;
using ChatApp.Domain.Entities;
using ChatApp.Domain.Enum;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace ChatApp.Application.Services
{
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

            // Xử lý đặc biệt cho Direct Chat
            if (chat.ChatType == ChatType.Direct)
            {
                // Trong direct chat, cả 2 người đều có quyền cơ bản
                return ChatPermissions.BasicMember;
            }

            // Với group chat, sử dụng role system
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

            // Xử lý đặc biệt cho Direct Chat
            if (chat.ChatType == ChatType.Direct)
            {
                // Trong direct chat, cả 2 người đều có quyền cơ bản
                return ChatPermissions.BasicMember;
            }

            // Với group chat, sử dụng role system
            return chat.GetRolePermissions(member.Role);
        }

        public async Task<ChatPermissions> GetRolePermissions(int chatId, ChatMemberRole role)
        {
            var chat = await _context.Chats
                .Include(c => c.RolePermissions)
                .FirstOrDefaultAsync(c => c.ChatId == chatId);

            if (chat == null)
                return ChatPermissions.None;

            // Xử lý đặc biệt cho Direct Chat
            if (chat.ChatType == ChatType.Direct)
            {
                // Direct chat không sử dụng role system, tất cả đều có quyền cơ bản
                return ChatPermissions.BasicMember;
            }

            return chat.GetRolePermissions(role);
        }

        public async Task<bool> UpdateRolePermissions(int chatId, ChatMemberRole role, ChatPermissions permissions, string updatedBy)
        {
            // Kiểm tra xem có phải direct chat không
            var chat = await _context.Chats.FirstOrDefaultAsync(c => c.ChatId == chatId);
            if (chat?.ChatType == ChatType.Direct)
            {
                // Không thể thay đổi permissions của direct chat
                return false;
            }

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