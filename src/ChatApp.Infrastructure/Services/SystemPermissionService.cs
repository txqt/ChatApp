using ChatApp.Application.Interfaces;
using ChatApp.Domain.Entities;
using ChatApp.Domain.Enum;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ChatApp.Infrastructure.Services
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

    public class SystemPermissionService : ISystemPermissionService
    {
        private readonly IApplicationDbContext _context;

        public SystemPermissionService(IApplicationDbContext context)
        {
            _context = context;
        }

        #region User Permissions

        public async Task<bool> CanUserPerformAction(string userId, AppPermissions permission)
        {
            var userPermissions = await GetUserPermissions(userId);
            return userPermissions.HasFlag(permission);
        }

        public async Task<AppPermissions> GetUserPermissions(string userId)
        {
            var user = await _context.Users
                .Include(u => u.UserPermission)
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                        .ThenInclude(r => r.RolePermission)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                return AppPermissions.None;

            // Bắt đầu với quyền cơ bản
            var permissions = AppPermissions.BasicUser;

            // Thêm quyền từ user permission (nếu có)
            if (user.UserPermission != null)
            {
                permissions = (AppPermissions)user.UserPermission.PermissionMask;
            }

            // Thêm quyền từ các role
            foreach (var userRole in user.UserRoles.Where(ur => ur.Role.IsActive))
            {
                if (userRole.Role.RolePermission != null)
                {
                    var rolePermissions = (AppPermissions)userRole.Role.RolePermission.PermissionMask;
                    permissions |= rolePermissions;
                }
            }

            return permissions;
        }

        public async Task<bool> UpdateUserPermissions(string userId, AppPermissions permissions, string updatedBy)
        {
            // Kiểm tra quyền của người cập nhật
            if (!await CanManageUsers(updatedBy))
                return false;

            var existingPermission = await _context.UserPermissions
                .FirstOrDefaultAsync(up => up.UserId == userId);

            if (existingPermission != null)
            {
                existingPermission.PermissionMask = (long)permissions;
                existingPermission.UpdatedAt = DateTime.UtcNow;
                existingPermission.UpdatedBy = updatedBy;
            }
            else
            {
                _context.UserPermissions.Add(new UserPermission
                {
                    UserId = userId,
                    PermissionMask = (long)permissions,
                    UpdatedBy = updatedBy
                });
            }

            await _context.SaveChangesAsync();
            await LogAuditAction(updatedBy, "UpdateUserPermissions", "UserPermission", userId,
                existingPermission?.PermissionMask.ToString(), permissions.ToString());

            return true;
        }

        public async Task<bool> GrantUserPermission(string userId, AppPermissions permission, string grantedBy)
        {
            var currentPermissions = await GetUserPermissions(userId);
            var newPermissions = currentPermissions | permission;
            return await UpdateUserPermissions(userId, newPermissions, grantedBy);
        }

        public async Task<bool> RevokeUserPermission(string userId, AppPermissions permission, string revokedBy)
        {
            var currentPermissions = await GetUserPermissions(userId);
            var newPermissions = currentPermissions & ~permission;
            return await UpdateUserPermissions(userId, newPermissions, revokedBy);
        }

        #endregion

        #region Role Permissions

        public async Task<AppPermissions> GetRolePermissions(string roleId)
        {
            var rolePermission = await _context.RolePermissions
                .FirstOrDefaultAsync(rp => rp.RoleId == roleId);

            return rolePermission != null
                ? (AppPermissions)rolePermission.PermissionMask
                : AppPermissions.BasicUser;
        }

        public async Task<bool> UpdateRolePermissions(string roleId, AppPermissions permissions, string updatedBy)
        {
            // Kiểm tra quyền của người cập nhật
            if (!await CanManageRoles(updatedBy))
                return false;

            var existingPermission = await _context.RolePermissions
                .FirstOrDefaultAsync(rp => rp.RoleId == roleId);

            if (existingPermission != null)
            {
                existingPermission.PermissionMask = (long)permissions;
                existingPermission.UpdatedAt = DateTime.UtcNow;
                existingPermission.UpdatedBy = updatedBy;
            }
            else
            {
                _context.RolePermissions.Add(new RolePermission
                {
                    RoleId = roleId,
                    PermissionMask = (long)permissions,
                    UpdatedBy = updatedBy
                });
            }

            await _context.SaveChangesAsync();
            await LogAuditAction(updatedBy, "UpdateRolePermissions", "RolePermission", roleId,
                existingPermission?.PermissionMask.ToString(), permissions.ToString());

            return true;
        }

        public async Task<bool> GrantRolePermission(string roleId, AppPermissions permission, string grantedBy)
        {
            var currentPermissions = await GetRolePermissions(roleId);
            var newPermissions = currentPermissions | permission;
            return await UpdateRolePermissions(roleId, newPermissions, grantedBy);
        }

        public async Task<bool> RevokeRolePermission(string roleId, AppPermissions permission, string revokedBy)
        {
            var currentPermissions = await GetRolePermissions(roleId);
            var newPermissions = currentPermissions & ~permission;
            return await UpdateRolePermissions(roleId, newPermissions, revokedBy);
        }

        #endregion

        #region Role Assignment

        public async Task<bool> AssignUserToRole(string userId, string roleId, string assignedBy)
        {
            // Kiểm tra quyền của người gán
            if (!await CanManageUsers(assignedBy))
                return false;

            // Kiểm tra role có tồn tại và active không
            var role = await _context.Roles.FirstOrDefaultAsync(r => r.Id == roleId && r.IsActive);
            if (role == null)
                return false;

            // Kiểm tra user đã có role này chưa
            var existingUserRole = await _context.UserRoles
                .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId);

            if (existingUserRole != null)
                return true; // Đã có rồi

            // Thêm role mới
            _context.UserRoles.Add(new ApplicationUserRole
            {
                UserId = userId,
                RoleId = roleId,
                AssignedBy = assignedBy,
                AssignedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            await LogAuditAction(assignedBy, "AssignUserToRole", "ApplicationUserRole", userId,
                null, $"RoleId: {roleId}");

            return true;
        }

        public async Task<bool> RemoveUserFromRole(string userId, string roleId, string removedBy)
        {
            // Kiểm tra quyền của người xóa
            if (!await CanManageUsers(removedBy))
                return false;

            var userRole = await _context.UserRoles
                .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId);

            if (userRole == null)
                return true; // Không có thì coi như đã xóa

            _context.UserRoles.Remove(userRole);
            await _context.SaveChangesAsync();
            await LogAuditAction(removedBy, "RemoveUserFromRole", "ApplicationUserRole", userId,
                $"RoleId: {roleId}", null);

            return true;
        }

        public async Task<List<ApplicationRole>> GetUserRoles(string userId)
        {
            return await _context.UserRoles
                .Where(ur => ur.UserId == userId)
                .Include(ur => ur.Role)
                .Where(ur => ur.Role.IsActive)
                .Select(ur => ur.Role)
                .ToListAsync();
        }

        public async Task<bool> IsUserInRole(string userId, string roleName)
        {
            return await _context.UserRoles
                .AnyAsync(ur => ur.UserId == userId &&
                               ur.Role.Name == roleName &&
                               ur.Role.IsActive);
        }

        #endregion

        #region System Admin Operations

        public async Task<bool> CanManageSystem(string userId)
        {
            return await CanUserPerformAction(userId, AppPermissions.ManageSystem);
        }

        public async Task<bool> CanManageUsers(string userId)
        {
            return await CanUserPerformAction(userId, AppPermissions.EditUser);
        }

        public async Task<bool> CanManageRoles(string userId)
        {
            return await CanUserPerformAction(userId, AppPermissions.ManageRoles);
        }

        public async Task<bool> CanViewAuditLogs(string userId)
        {
            return await CanUserPerformAction(userId, AppPermissions.ViewSystemLogs);
        }

        #endregion

        #region Private Methods

        private async Task LogAuditAction(string userId, string action, string entityType, string? entityId,
            string? oldValues, string? newValues)
        {
            await _context.AuditLogs.AddAsync(new AuditLog
            {
                UserId = userId,
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                OldValues = oldValues,
                NewValues = newValues,
                Timestamp = DateTime.UtcNow
            });
        }

        #endregion
    }
}