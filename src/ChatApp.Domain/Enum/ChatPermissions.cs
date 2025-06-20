using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatApp.Domain.Enum
{
    [Flags]
    public enum ChatPermissions : long
    {
        None = 0,

        // ===== BASIC CHAT ACTIONS =====
        ViewMessages = 1L << 0,       // Xem tin nhắn
        SendMessages = 1L << 1,       // Gửi tin nhắn text
        SendMedia = 1L << 2,          // Gửi file/hình ảnh
        SendVoice = 1L << 3,          // Gửi voice message
        React = 1L << 4,              // Thả react tin nhắn
        ForwardMessages = 1L << 5,    // Chuyển tiếp tin nhắn

        // ===== MESSAGE MANAGEMENT =====
        DeleteOwnMessages = 1L << 6,  // Xóa tin nhắn của mình
        EditOwnMessages = 1L << 7,    // Sửa tin nhắn của mình
        DeleteAnyMessage = 1L << 8,   // Xóa tin nhắn của người khác
        EditAnyMessage = 1L << 9,     // Sửa tin nhắn của người khác
        PinMessages = 1L << 10,       // Ghim tin nhắn

        // ===== MEMBER MANAGEMENT =====
        ViewMembers = 1L << 11,       // Xem danh sách thành viên
        AddMembers = 1L << 12,        // Thêm thành viên
        RemoveMembers = 1L << 13,     // Kick thành viên
        MuteMembers = 1L << 14,       // Tắt tiếng thành viên
        ChangeNicknames = 1L << 15,   // Đổi nickname thành viên

        // ===== GROUP SETTINGS =====
        EditGroupInfo = 1L << 16,     // Sửa tên, avatar, mô tả nhóm
        ManageRoles = 1L << 17,       // Tạo/sửa/xóa role trong nhóm
        ManagePermissions = 1L << 18, // Phân quyền cho thành viên/role

        // ===== ADVANCED FEATURES =====
        CreatePolls = 1L << 19,       // Tạo poll/bình chọn
        ViewMessageHistory = 1L << 20, // Xem lịch sử tin nhắn đầy đủ
        ExportChat = 1L << 21,        // Export dữ liệu chat

        // ===== DESTRUCTIVE ACTIONS =====
        DeleteGroup = 1L << 22,       // Xóa toàn bộ nhóm

        // ===== PREDEFINED ROLE COMBINATIONS =====
        ReadOnly = ViewMessages | ViewMembers,

        BasicMember = ViewMessages | SendMessages | SendMedia | React |
                     DeleteOwnMessages | EditOwnMessages | ViewMembers | ForwardMessages | ViewMessageHistory,

        Moderator = BasicMember | DeleteAnyMessage | PinMessages |
                   MuteMembers | ChangeNicknames | ViewMessageHistory,

        Admin = Moderator | AddMembers | RemoveMembers | EditGroupInfo |
               ManageRoles | EditAnyMessage | ExportChat,

        Owner = Admin | ManagePermissions | DeleteGroup,

        All = long.MaxValue
    }
}
