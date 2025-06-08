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

        // Basic permissions
        ViewMessages = 1 << 0,      // Xem tin nhắn
        SendMessages = 1 << 1,      // Gửi tin nhắn
        SendMedia = 1 << 2,         // Gửi file/hình ảnh
        DeleteOwnMessages = 1 << 3, // Xóa tin nhắn của mình
        EditOwnMessages = 1 << 4,   // Sửa tin nhắn của mình

        // Moderation permissions
        DeleteAnyMessage = 1 << 5,  // Xóa tin nhắn của người khác
        PinMessages = 1 << 6,       // Ghim tin nhắn

        // Member management
        AddMembers = 1 << 7,        // Thêm thành viên
        RemoveMembers = 1 << 8,     // Xóa thành viên
        MuteMembers = 1 << 9,       // Tắt tiếng thành viên

        // Group management
        EditGroupInfo = 1 << 10,    // Sửa thông tin nhóm
        ManageRoles = 1 << 11,      // Quản lý role

        // Admin permissions
        ManagePermissions = 1 << 12, // Quản lý quyền
        DeleteGroup = 1 << 13,       // Xóa nhóm

        // Convenience combinations
        BasicUser = ViewMessages | SendMessages | SendMedia | DeleteOwnMessages | EditOwnMessages,
        Moderator = BasicUser | DeleteAnyMessage | PinMessages | MuteMembers,
        Admin = Moderator | AddMembers | RemoveMembers | EditGroupInfo | ManageRoles,
        Owner = Admin | ManagePermissions | DeleteGroup,
        All = long.MaxValue
    }
}
