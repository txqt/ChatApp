using ChatApp.Application.Services;
using ChatApp.Domain.Enum;
using System;
using System.Threading.Tasks;

namespace ChatApp.Application.Extensions
{
    public static class PermissionExtensions
    {
        /// <summary>
        /// Kiểm tra có quyền hay không, throw exception nếu không có
        /// </summary>
        public static async Task RequirePermission(this IUnifiedPermissionService permissionService,
            int userId, int? chatId, string action)
        {
            var hasPermission = await permissionService.HasPermission(userId, chatId, action);
            if (!hasPermission)
            {
                throw new UnauthorizedAccessException($"User {userId} không có quyền thực hiện action: {action}");
            }
        }

        /// <summary>
        /// Kiểm tra system permission
        /// </summary>
        public static async Task<bool> HasSystemPermission(this ISystemPermissionService permissionService,
            int userId, AppPermissions permission)
        {
            return await permissionService.CanUserPerformAction(userId, permission);
        }

        /// <summary>
        /// Kiểm tra chat permission
        /// </summary>
        public static async Task<bool> HasChatPermission(this IChatPermissionService permissionService,
            int userId, int chatId, ChatPermissions permission)
        {
            return await permissionService.CanUserPerformAction(userId, chatId, permission);
        }

        /// <summary>
        /// Toggle permission on/off
        /// </summary>
        public static async Task<bool> ToggleSystemPermission(this ISystemPermissionService permissionService,
            int userId, AppPermissions permission, int updatedBy)
        {
            var currentPermissions = await permissionService.GetUserPermissions(userId);

            if (currentPermissions.HasFlag(permission))
            {
                // Tắt permission
                return await permissionService.RevokeUserPermission(userId, permission, updatedBy);
            }
            else
            {
                // Bật permission
                return await permissionService.GrantUserPermission(userId, permission, updatedBy);
            }
        }

        /// <summary>
        /// Toggle chat role permission
        /// </summary>
        public static async Task<bool> ToggleChatRolePermission(this IChatPermissionService permissionService,
            int chatId, ChatMemberRole role, ChatPermissions permission, int updatedBy)
        {
            var currentPermissions = await permissionService.GetRolePermissions(chatId, role);

            if (currentPermissions.HasFlag(permission))
            {
                // Tắt permission
                var newPermissions = currentPermissions & ~permission;
                return await permissionService.UpdateRolePermissions(chatId, role, newPermissions, updatedBy);
            }
            else
            {
                // Bật permission
                var newPermissions = currentPermissions | permission;
                return await permissionService.UpdateRolePermissions(chatId, role, newPermissions, updatedBy);
            }
        }

        /// <summary>
        /// Get permission display name
        /// </summary>
        private static readonly Dictionary<AppPermissions, string> _displayNames = new()
        {
            { AppPermissions.SendMessage, "Gửi tin nhắn" },
            { AppPermissions.CreateDirectChat, "Tạo cuộc trò chuyện riêng" },
            { AppPermissions.CreateGroup, "Tạo nhóm" },
            { AppPermissions.JoinGroup, "Tham gia nhóm" },
            { AppPermissions.UploadFile, "Tải lên tệp" },
            { AppPermissions.DownloadFile, "Tải xuống tệp" },
            { AppPermissions.DeleteOwnFile, "Xóa tệp của mình" },
            { AppPermissions.AddGroupMember, "Thêm thành viên" },
            { AppPermissions.RemoveGroupMember, "Xóa thành viên" },
            { AppPermissions.DeleteGroup, "Xóa nhóm" },
            { AppPermissions.EditGroupInfo, "Chỉnh sửa thông tin nhóm" },
            { AppPermissions.DeleteOwnMessage, "Xóa tin nhắn của mình" },
            { AppPermissions.DeleteAnyMessage, "Xóa bất kỳ tin nhắn nào" },
            { AppPermissions.EditOwnMessage, "Chỉnh sửa tin nhắn của mình" },
            { AppPermissions.EditAnyMessage, "Chỉnh sửa bất kỳ tin nhắn nào" },
            { AppPermissions.MuteUser, "Tắt tiếng người dùng" },
            { AppPermissions.BanUser, "Cấm người dùng" },
            { AppPermissions.ViewMessageHistory, "Xem lịch sử tin nhắn" },
            { AppPermissions.ManageUsers, "Quản lý người dùng" },
            { AppPermissions.ManageRoles, "Quản lý vai trò" },
            { AppPermissions.ViewSystemLogs, "Xem nhật ký hệ thống" },
            { AppPermissions.ManageSystem, "Quản lý hệ thống" }
        };

        public static List<string> GetDisplayNames(this AppPermissions permissions)
        {
            if (permissions == AppPermissions.None)
                return new List<string> { "Không có quyền" };

            var result = new List<string>();

            foreach (var kv in _displayNames)
            {
                if (permissions.HasFlag(kv.Key))
                    result.Add(kv.Value);
            }

            return result;
        }

        public static string GetDisplayString(this AppPermissions permissions, string separator = ", ")
        {
            return string.Join(separator, permissions.GetDisplayNames());
        }

        /// <summary>
        /// Get chat permission display name
        /// </summary>
        public static string GetDisplayName(this ChatPermissions permission)
        {
            return permission switch
            {
                ChatPermissions.ViewMessages => "Xem tin nhắn",
                ChatPermissions.SendMessages => "Gửi tin nhắn",
                ChatPermissions.SendMedia => "Gửi media",
                ChatPermissions.DeleteOwnMessages => "Xóa tin nhắn của mình",
                ChatPermissions.EditOwnMessages => "Sửa tin nhắn của mình",
                ChatPermissions.DeleteAnyMessage => "Xóa bất kỳ tin nhắn nào",
                ChatPermissions.PinMessages => "Ghim tin nhắn",
                ChatPermissions.AddMembers => "Thêm thành viên",
                ChatPermissions.RemoveMembers => "Xóa thành viên",
                ChatPermissions.MuteMembers => "Tắt tiếng thành viên",
                ChatPermissions.EditGroupInfo => "Sửa thông tin nhóm",
                ChatPermissions.ManageRoles => "Quản lý vai trò",
                ChatPermissions.ManagePermissions => "Quản lý quyền",
                ChatPermissions.DeleteGroup => "Xóa nhóm",
                _ => permission.ToString()
            };
        }

        /// <summary>
        /// Check if permission is critical (cần cẩn thận khi grant/revoke)
        /// </summary>
        public static bool IsCritical(this AppPermissions permission)
        {
            return permission.HasFlag(AppPermissions.ManageSystem) ||
                   permission.HasFlag(AppPermissions.SuperAdmin) ||
                   permission.HasFlag(AppPermissions.ManageUsers) ||
                   permission.HasFlag(AppPermissions.ManageRoles);
        }

        /// <summary>
        /// Check if chat permission is critical
        /// </summary>
        public static bool IsCritical(this ChatPermissions permission)
        {
            return permission.HasFlag(ChatPermissions.ManagePermissions) ||
                   permission.HasFlag(ChatPermissions.DeleteGroup) ||
                   permission.HasFlag(ChatPermissions.ManageRoles);
        }
    }
}