using ChatApp.Domain.Entities;
using ChatApp.Domain.Enum;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatApp.Infrastructure.Data
{
    public static class DbInitializer
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

            // Ensure database is created
            await context.Database.EnsureCreatedAsync();

            // Seed roles
            await SeedRolesAsync(roleManager, context);

            // Seed admin user
            await SeedAdminUserAsync(userManager, context);
        }

        private static async Task SeedRolesAsync(RoleManager<ApplicationRole> roleManager, ApplicationDbContext context)
        {
            var roles = new[]
            {
            new { Name = "SuperAdmin", Description = "Super Administrator with all permissions", Permission = AppPermissions.SuperAdmin },
            new { Name = "Admin", Description = "Administrator", Permission = AppPermissions.Administrator },
            new { Name = "Moderator", Description = "Group Moderator", Permission = AppPermissions.GroupModerator },
            new { Name = "User", Description = "Basic User", Permission = AppPermissions.BasicUser }
        };

            foreach (var roleInfo in roles)
            {
                var role = await roleManager.FindByNameAsync(roleInfo.Name);
                if (role == null)
                {
                    role = new ApplicationRole
                    {
                        Name = roleInfo.Name,
                        NormalizedName = roleInfo.Name.ToUpper(),
                        Description = roleInfo.Description
                    };

                    var result = await roleManager.CreateAsync(role);
                    if (!result.Succeeded)
                    {
                        throw new Exception($"Failed to create role {roleInfo.Name}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                    }
                }
                else
                {
                    // Update description if changed
                    if (role.Description != roleInfo.Description)
                    {
                        role.Description = roleInfo.Description;
                        await roleManager.UpdateAsync(role);
                    }
                }

                // Ensure RolePermission exists and is up to date
                var dbRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == roleInfo.Name);
                if (dbRole != null)
                {
                    var rolePermission = await context.RolePermissions.FirstOrDefaultAsync(rp => rp.RoleId == dbRole.Id);
                    if (rolePermission == null)
                    {
                        rolePermission = new RolePermission
                        {
                            RoleId = dbRole.Id,
                            PermissionMask = (long)roleInfo.Permission
                        };
                        context.RolePermissions.Add(rolePermission);
                    }
                    else if (rolePermission.PermissionMask != (long)roleInfo.Permission)
                    {
                        rolePermission.PermissionMask = (long)roleInfo.Permission;
                        context.RolePermissions.Update(rolePermission);
                    }
                }
            }

            await context.SaveChangesAsync();
        }
        private static async Task SeedAdminUserAsync(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            const string adminEmail = "admin@chatapp.local";
            const string adminUserName = "admin";
            const string adminPassword = "1";
            const string adminRole = "SuperAdmin";

            var adminUser = await userManager.FindByNameAsync(adminUserName);
            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminUserName,
                    Email = adminEmail,
                    EmailConfirmed = true,
                    DisplayName = "Administrator",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    IsOnline = false
                };

                var result = await userManager.CreateAsync(adminUser, adminPassword);
                if (!result.Succeeded)
                {
                    throw new Exception($"Failed to create admin user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }
            }
            else
            {
                // Ensure admin user is active and has correct display name
                bool changed = false;
                if (!adminUser.IsActive) { adminUser.IsActive = true; changed = true; }
                if (adminUser.DisplayName != "Administrator") { adminUser.DisplayName = "Administrator"; changed = true; }
                if (changed)
                {
                    adminUser.UpdatedAt = DateTime.UtcNow;
                    await userManager.UpdateAsync(adminUser);
                }
            }

            // Ensure admin user is in SuperAdmin role
            if (!await userManager.IsInRoleAsync(adminUser, adminRole))
            {
                await userManager.AddToRoleAsync(adminUser, adminRole);
            }

            // Ensure UserPermission exists and is up to date
            var userPermission = await context.UserPermissions.FirstOrDefaultAsync(up => up.UserId == adminUser.Id);
            var superAdminPermissionMask = (long)ChatApp.Domain.Enum.AppPermissions.SuperAdmin;
            if (userPermission == null)
            {
                userPermission = new UserPermission
                {
                    UserId = adminUser.Id,
                    PermissionMask = superAdminPermissionMask
                };
                context.UserPermissions.Add(userPermission);
                await context.SaveChangesAsync();
            }
            else if (userPermission.PermissionMask != superAdminPermissionMask)
            {
                userPermission.PermissionMask = superAdminPermissionMask;
                context.UserPermissions.Update(userPermission);
                await context.SaveChangesAsync();
            }
        }
    }
}