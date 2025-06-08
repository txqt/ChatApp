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

        // ===== BASIC CHAT PERMISSIONS =====
        SendMessage = 1L << 0,        // 0x0001 - Send messages in chats
        CreateDirectChat = 1L << 1,   // 0x0002 - Create 1-1 conversations
        CreateGroup = 1L << 2,        // 0x0004 - Create group chats
        JoinGroup = 1L << 3,          // 0x0008 - Join existing groups

        // ===== FILE PERMISSIONS =====
        UploadFile = 1L << 4,         // 0x0010 - Upload files/media
        DownloadFile = 1L << 5,       // 0x0020 - Download files/media
        DeleteOwnFile = 1L << 6,      // 0x0040 - Delete own uploaded files

        // ===== GROUP MANAGEMENT =====
        AddGroupMember = 1L << 7,     // 0x0080 - Add members to groups
        RemoveGroupMember = 1L << 8,  // 0x0100 - Remove members from groups
        DeleteGroup = 1L << 9,        // 0x0200 - Delete entire groups
        EditGroupInfo = 1L << 10,     // 0x0400 - Edit group name, description

        // ===== MESSAGE MANAGEMENT =====
        DeleteOwnMessage = 1L << 11,  // 0x0800 - Delete own messages
        DeleteAnyMessage = 1L << 12,  // 0x1000 - Delete any message in group
        EditOwnMessage = 1L << 13,    // 0x2000 - Edit own messages
        EditAnyMessage = 1L << 14,    // 0x4000 - Edit any message (admin)

        // ===== MODERATION PERMISSIONS =====
        MuteUser = 1L << 15,          // 0x8000 - Mute users in groups
        BanUser = 1L << 16,           // 0x10000 - Ban users from groups
        ViewMessageHistory = 1L << 17, // 0x20000 - View full message history

        // ===== ADMIN PERMISSIONS =====
        ManageUsers = 1L << 18,       // 0x40000 - Manage user accounts
        ManageRoles = 1L << 19,       // 0x80000 - Manage roles and permissions
        ViewSystemLogs = 1L << 20,    // 0x100000 - View system audit logs
        ManageSystem = 1L << 21,      // 0x200000 - System configuration
        DeleteAnyChat = 1L << 22,     // 0x400000 - Delete any chat (admin)

        // ===== PREDEFINED ROLE COMBINATIONS =====
        BasicUser = SendMessage | CreateDirectChat | JoinGroup |
                   UploadFile | DownloadFile | DeleteOwnMessage |
                   EditOwnMessage | DeleteOwnFile,

        GroupModerator = BasicUser | CreateGroup | AddGroupMember |
                        RemoveGroupMember | EditGroupInfo |
                        DeleteAnyMessage | MuteUser | ViewMessageHistory,

        Administrator = GroupModerator | DeleteGroup | EditAnyMessage |
                       BanUser | ManageUsers | ManageRoles |
                       ViewSystemLogs | ManageSystem | DeleteAnyChat,

        SuperAdmin = Administrator | long.MaxValue // All permissions
    }
}
