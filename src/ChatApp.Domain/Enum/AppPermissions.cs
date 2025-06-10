using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatApp.Domain.Enum
{
    [Flags]
    public enum AppPermissions : long
    {
        None = 0,
        // ===== USER MANAGEMENT =====
        ViewUsers = 1L << 0,          // Xem danh sách user
        CreateUser = 1L << 1,         // Tạo user mới
        EditUser = 1L << 2,           // Chỉnh sửa thông tin user
        DeleteUser = 1L << 3,         // Xóa user
        BanUser = 1L << 4,            // Ban user khỏi hệ thống

        // ===== SYSTEM MANAGEMENT =====
        ViewSystemLogs = 1L << 5,     // Xem log hệ thống
        ManageSystem = 1L << 6,       // Quản lý cấu hình hệ thống
        ManageRoles = 1L << 7,        // Quản lý role toàn hệ thống
        ViewAnalytics = 1L << 8,      // Xem thống kê hệ thống

        // ===== GLOBAL CHAT MANAGEMENT =====
        CreateDirectChat = 1L << 9,        // Tạo group chat mới
        CreateGroup = 1L << 10,        // Tạo group chat mới
        ViewAllChats = 1L << 11,      // Xem tất cả chat
        DeleteAnyChat = 1L << 12,     // Xóa bất kỳ chat nào

        // ===== PREDEFINED COMBINATIONS =====
        BasicUser = CreateDirectChat | CreateGroup,
        Moderator = BasicUser | ViewUsers | BanUser | ViewAllChats,
        Administrator = Moderator | CreateUser | EditUser | DeleteUser |
                       ViewSystemLogs | ManageRoles | DeleteAnyChat,
        SuperAdmin = Administrator | ManageSystem | ViewAnalytics
    }
}
