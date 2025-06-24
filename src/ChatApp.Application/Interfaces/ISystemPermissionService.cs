using ChatApp.Domain.Entities;
using ChatApp.Domain.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatApp.Application.Interfaces
{
    public interface ISystemPermissionService
    {
        // User permissions
        Task<bool> CanUserPerformAction(string userId, AppPermissions permission);
        Task<AppPermissions> GetUserPermissions(string userId);
        Task<bool> UpdateUserPermissions(string userId, AppPermissions permissions, string updatedBy);
        Task<bool> GrantUserPermission(string userId, AppPermissions permission, string grantedBy);
        Task<bool> RevokeUserPermission(string userId, AppPermissions permission, string revokedBy);

        // Role permissions
        Task<AppPermissions> GetRolePermissions(string roleId);
        Task<bool> UpdateRolePermissions(string roleId, AppPermissions permissions, string updatedBy);
        Task<bool> GrantRolePermission(string roleId, AppPermissions permission, string grantedBy);
        Task<bool> RevokeRolePermission(string roleId, AppPermissions permission, string revokedBy);

        // Role assignment
        Task<bool> AssignUserToRole(string userId, string roleId, string assignedBy);
        Task<bool> RemoveUserFromRole(string userId, string roleId, string removedBy);
        Task<List<ApplicationRole>> GetUserRoles(string userId);
        Task<bool> IsUserInRole(string userId, string roleName);

        // System admin operations
        Task<bool> CanManageSystem(string userId);
        Task<bool> CanManageUsers(string userId);
        Task<bool> CanManageRoles(string userId);
        Task<bool> CanViewAuditLogs(string userId);
    }
}
