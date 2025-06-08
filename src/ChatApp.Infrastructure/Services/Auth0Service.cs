using ChatApp.Application.Interfaces;
using ChatApp.Domain.Entities;
using ChatApp.Domain.Enum;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ChatApp.Infrastructure.Services
{
    public interface IAuth0Service
    {
        Task<ApplicationUser> SyncUserAsync(ClaimsPrincipal claimsPrincipal);
    }

    public class Auth0Service : IAuth0Service
    {
        private readonly IApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public Auth0Service(
            IApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<ApplicationUser> SyncUserAsync(ClaimsPrincipal claimsPrincipal)
        {
            var auth0Id = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? claimsPrincipal.FindFirst("sub")?.Value;
            var email = claimsPrincipal.FindFirst(ClaimTypes.Email)?.Value;
            var name = claimsPrincipal.FindFirst("name")?.Value;
            var picture = claimsPrincipal.FindFirst("picture")?.Value;

            if (string.IsNullOrEmpty(auth0Id))
                throw new ArgumentException("Auth0 ID not found in claims");

            // Tìm user existing
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.UserName == auth0Id);

            if (existingUser != null)
            {
                // Update thông tin nếu có thay đổi
                var updated = false;

                if (!string.IsNullOrEmpty(email) && existingUser.Email != email)
                {
                    existingUser.Email = email;
                    existingUser.NormalizedEmail = email.ToUpper();
                    updated = true;
                }

                if (!string.IsNullOrEmpty(name) && existingUser.DisplayName != name)
                {
                    existingUser.DisplayName = name;
                    updated = true;
                }

                if (!string.IsNullOrEmpty(picture) && existingUser.AvatarUrl != picture)
                {
                    existingUser.AvatarUrl = picture;
                    updated = true;
                }

                if (updated)
                {
                    existingUser.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }

                return existingUser;
            }

            // Tạo user mới
            var newUser = new ApplicationUser
            {
                UserName = email,
                NormalizedUserName = email?.ToUpper(),
                Email = email,
                NormalizedEmail = email?.ToUpper(),
                EmailConfirmed = true, // Auth0 đã verify
                DisplayName = name ?? email ?? "Unknown User",
                Auth0Id = auth0Id,
                AvatarUrl = picture,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(newUser);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException($"Failed to create user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }

            // Gán role mặc định
            await AssignDefaultRoleAsync(newUser);

            // Tạo default permissions
            await CreateDefaultPermissionsAsync(newUser);

            return newUser;
        }


        private async Task AssignDefaultRoleAsync(ApplicationUser user)
        {
            // Tìm hoặc tạo role "User"
            var userRole = await _context.Roles
                .FirstOrDefaultAsync(r => r.Name == "User");

            if (userRole == null)
            {
                userRole = new ApplicationRole
                {
                    Name = "User",
                    NormalizedName = "USER",
                    Description = "Default user role",
                    IsActive = true
                };
                _context.Roles.Add(userRole);
                await _context.SaveChangesAsync();

                // Tạo permission cho role
                var rolePermission = new RolePermission
                {
                    RoleId = userRole.Id,
                    PermissionMask = (long)AppPermissions.BasicUser
                };
                _context.RolePermissions.Add(rolePermission);
            }

            // Gán role cho user
            var userRoleAssignment = new ApplicationUserRole
            {
                UserId = user.Id,
                RoleId = userRole.Id,
                AssignedAt = DateTime.UtcNow
            };

            _context.UserRoles.Add(userRoleAssignment);
            await _context.SaveChangesAsync();
        }

        private async Task CreateDefaultPermissionsAsync(ApplicationUser user)
        {
            var userPermission = new UserPermission
            {
                UserId = user.Id,
                PermissionMask = (long)AppPermissions.BasicUser,
                UpdatedAt = DateTime.UtcNow
            };

            _context.UserPermissions.Add(userPermission);
            await _context.SaveChangesAsync();
        }
    }
}