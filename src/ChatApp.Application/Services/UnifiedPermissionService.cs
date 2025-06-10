//using ChatApp.Application.Interfaces;
//using ChatApp.Domain.Entities;
//using ChatApp.Domain.Enum;
//using Microsoft.EntityFrameworkCore;
//using System.Threading.Tasks;

//namespace ChatApp.Application.Services
//{
//    public interface IUnifiedPermissionService
//    {
//        // System level checks
//        Task<bool> CanCreateChat(int userId);
//        Task<bool> CanJoinChat(int userId, int chatId);
//        Task<bool> CanDeleteChat(int userId, int chatId);
//        Task<bool> CanManageSystemSettings(int userId);

//        // Chat level checks
//        Task<bool> CanSendMessage(int userId, int chatId);
//        Task<bool> CanDeleteMessage(int userId, int chatId, int messageId);
//        Task<bool> CanAddMember(int userId, int chatId);
//        Task<bool> CanRemoveMember(int userId, int chatId, int targetUserId);
//        Task<bool> CanManageRoles(int userId, int chatId);

//        // Combined permission check
//        Task<bool> HasPermission(int userId, int? chatId, string action);

//        // User status checks
//        Task<bool> IsUserActive(int userId);
//        Task<bool> IsUserBanned(int userId);
//    }

//    public class UnifiedPermissionService : IUnifiedPermissionService
//    {
//        private readonly ISystemPermissionService _systemPermissionService;
//        private readonly IChatPermissionService _chatPermissionService;
//        private readonly IApplicationDbContext _context;

//        public UnifiedPermissionService(
//            ISystemPermissionService systemPermissionService,
//            IChatPermissionService chatPermissionService,
//            IApplicationDbContext context)
//        {
//            _systemPermissionService = systemPermissionService;
//            _chatPermissionService = chatPermissionService;
//            _context = context;
//        }

//        #region System Level Checks

//        public async Task<bool> CanCreateChat(int userId)
//        {
//            // Kiểm tra user có active không
//            if (!await IsUserActive(userId))
//                return false;

//            // Kiểm tra quyền hệ thống
//            return await _systemPermissionService.CanUserPerformAction(userId, AppPermissions.CreateDirectChat);
//        }

//        public async Task<bool> CanJoinChat(int userId, int chatId)
//        {
//            // Kiểm tra user có active không
//            if (!await IsUserActive(userId))
//                return false;

//            // Kiểm tra chat có tồn tại và active không
//            var chat = await _context.Chats.FirstOrDefaultAsync(c => c.ChatId == chatId && c.IsActive);
//            if (chat == null)
//                return false;

//            // Kiểm tra đã là member chưa
//            var existingMember = await _context.ChatMembers
//                .FirstOrDefaultAsync(cm => cm.ChatId == chatId && cm.UserId == userId && cm.IsActive);
//            if (existingMember != null)
//                return true; // Đã là member rồi

//            // Kiểm tra quyền hệ thống join chat
//            return await _systemPermissionService.CanUserPerformAction(userId, AppPermissions.JoinGroup);
//        }

//        public async Task<bool> CanDeleteChat(int userId, int chatId)
//        {
//            // Kiểm tra user có active không
//            if (!await IsUserActive(userId))
//                return false;

//            // Admin hệ thống có thể xóa bất kỳ chat nào
//            if (await _systemPermissionService.CanUserPerformAction(userId, AppPermissions.DeleteAnyChat))
//                return true;

//            // Owner chat có thể xóa chat của mình
//            return await _chatPermissionService.CanUserPerformAction(userId, chatId, ChatPermissions.DeleteGroup);
//        }

//        public async Task<bool> CanManageSystemSettings(int userId)
//        {
//            return await _systemPermissionService.CanManageSystem(userId);
//        }

//        #endregion

//        #region Chat Level Checks

//        public async Task<bool> CanSendMessage(int userId, int chatId)
//        {
//            // Kiểm tra user có active không
//            if (!await IsUserActive(userId))
//                return false;

//            // Kiểm tra có bị banned không
//            if (await IsUserBanned(userId))
//                return false;

//            // Kiểm tra quyền hệ thống
//            if (!await _systemPermissionService.CanUserPerformAction(userId, AppPermissions.SendMessage))
//                return false;

//            // Kiểm tra quyền trong chat
//            return await _chatPermissionService.CanUserPerformAction(userId, chatId, ChatPermissions.SendMessages);
//        }

//        public async Task<bool> CanDeleteMessage(int userId, int chatId, int messageId)
//        {
//            // Kiểm tra user có active không
//            if (!await IsUserActive(userId))
//                return false;

//            var message = await _context.Messages
//                .FirstOrDefaultAsync(m => m.MessageId == messageId && m.ChatId == chatId && !m.IsDeleted);

//            if (message == null)
//                return false;

//            // Admin hệ thống có thể xóa bất kỳ tin nhắn nào
//            if (await _systemPermissionService.CanUserPerformAction(userId, AppPermissions.DeleteAnyMessage))
//                return true;

//            // Người gửi có thể xóa tin nhắn của mình
//            if (message.SenderId == userId)
//            {
//                return await _chatPermissionService.CanUserPerformAction(userId, chatId, ChatPermissions.DeleteOwnMessages);
//            }

//            // Moderator có thể xóa tin nhắn của người khác
//            return await _chatPermissionService.CanUserPerformAction(userId, chatId, ChatPermissions.DeleteAnyMessage);
//        }

//        public async Task<bool> CanAddMember(int userId, int chatId)
//        {
//            // Kiểm tra user có active không
//            if (!await IsUserActive(userId))
//                return false;

//            // Kiểm tra quyền hệ thống
//            if (!await _systemPermissionService.CanUserPerformAction(userId, AppPermissions.AddGroupMember))
//                return false;

//            // Kiểm tra quyền trong chat
//            return await _chatPermissionService.CanUserPerformAction(userId, chatId, ChatPermissions.AddMembers);
//        }

//        public async Task<bool> CanRemoveMember(int userId, int chatId, int targetUserId)
//        {
//            // Kiểm tra user có active không
//            if (!await IsUserActive(userId))
//                return false;

//            // Admin hệ thống có thể remove bất kỳ ai
//            if (await _systemPermissionService.CanUserPerformAction(userId, AppP))
//                return true;

//            // Không thể remove owner
//            var targetMember = await _context.ChatMembers
//                .FirstOrDefaultAsync(cm => cm.ChatId == chatId && cm.UserId == targetUserId && cm.IsActive);

//            if (targetMember?.Role == ChatMemberRole.Owner)
//                return false;

//            // Kiểm tra quyền trong chat
//            return await _chatPermissionService.CanUserPerformAction(userId, chatId, ChatPermissions.RemoveMembers);
//        }

//        public async Task<bool> CanManageRoles(int userId, int chatId)
//        {
//            // Kiểm tra user có active không
//            if (!await IsUserActive(userId))
//                return false;

//            // Admin hệ thống có thể manage roles
//            if (await _systemPermissionService.CanUserPerformAction(userId, AppPermissions.ManageRoles))
//                return true;

//            // Kiểm tra quyền trong chat
//            return await _chatPermissionService.CanUserPerformAction(userId, chatId, ChatPermissions.ManageRoles);
//        }

//        #endregion

//        #region Combined Permission Check

//        public async Task<bool> HasPermission(int userId, int? chatId, string action)
//        {
//            return action.ToLower() switch
//            {
//                "create_chat" => await CanCreateChat(userId),
//                "send_message" => chatId.HasValue && await CanSendMessage(userId, chatId.Value),
//                "delete_chat" => chatId.HasValue && await CanDeleteChat(userId, chatId.Value),
//                "add_member" => chatId.HasValue && await CanAddMember(userId, chatId.Value),
//                "manage_system" => await CanManageSystemSettings(userId),
//                "manage_roles" => chatId.HasValue && await CanManageRoles(userId, chatId.Value),
//                _ => false
//            };
//        }

//        #endregion

//        #region User Status Checks

//        public async Task<bool> IsUserActive(int userId)
//        {
//            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
//            return user?.IsActive == true;
//        }

//        public async Task<bool> IsUserBanned(int userId)
//        {
//            // Kiểm tra có role "Banned" không
//            return await _systemPermissionService.IsUserInRole(userId, "Banned");
//        }

//        #endregion
//    }
//}