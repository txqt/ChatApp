using ChatApp.Domain.Enum;
using ChatApp.Infrastructure.Services;
using System;
using System.Threading.Tasks;

namespace ChatApp.WebAPI.Extensions
{
    public static class PermissionExtensions
    {
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
            { AppPermissions.None, "Không có" },
            { AppPermissions.ViewUsers, "Xem người dùng" },
            { AppPermissions.CreateUser, "Tạo người dùng mới" },
            { AppPermissions.EditUser, "Chỉnh sửa người dùng" },
            { AppPermissions.DeleteUser, "Xóa người dùng" },
            { AppPermissions.BanUser, "Cấm người dùng" },
            { AppPermissions.ViewSystemLogs, "Xem nhật ký hệ thống" },
            { AppPermissions.ManageSystem, "Quản lý hệ thống" },
            { AppPermissions.ManageRoles, "Quản lý vai trò" },
            { AppPermissions.ViewAnalytics, "Xem thống kê hệ thống" },
            { AppPermissions.CreateDirectChat, "Tạo cuộc trò chuyện riêng" },
            { AppPermissions.CreateGroup, "Tạo nhóm" },
            { AppPermissions.ViewAllChats, "Xem tất cả cuộc trò chuyện" },
            { AppPermissions.DeleteAnyChat, "Xóa bất kỳ cuộc trò chuyện nào" },
            { AppPermissions.BasicUser, "Người dùng cơ bản" },
            { AppPermissions.Moderator, "Điều hành viên" },
            { AppPermissions.Administrator, "Quản trị viên" },
            { AppPermissions.SuperAdmin, "Super Admin" }
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
                   permission.HasFlag(AppPermissions.EditUser) ||
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