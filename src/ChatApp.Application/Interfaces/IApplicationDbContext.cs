using ChatApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatApp.Application.Interfaces
{
    public interface IApplicationDbContext
    {
        // Identity tables
        DbSet<ApplicationUser> Users { get; set; }
        DbSet<ApplicationRole> Roles { get; set; }
        DbSet<ApplicationUserRole> UserRoles { get; set; }

        // Permission system
        DbSet<UserPermission> UserPermissions { get; set; }
        DbSet<RolePermission> RolePermissions { get; set; }
        public DbSet<ChatRolePermission> ChatRolePermissions { get; set; }

        // Chat system
        DbSet<Chat> Chats { get; set; }
        DbSet<ChatMember> ChatMembers { get; set; }
        DbSet<Message> Messages { get; set; }
        DbSet<MessageStatus> MessageStatuses { get; set; }
        DbSet<MediaFile> MediaFiles { get; set; }

        // Audit
        DbSet<AuditLog> AuditLogs { get; set; }

        // Relationships
        public DbSet<Friendship> Friendships { get; set; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
