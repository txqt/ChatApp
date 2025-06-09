using ChatApp.Application.Interfaces;
using ChatApp.Domain.Entities;
using ChatApp.Domain.Enum;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatApp.Infrastructure.Data
{
    public class ApplicationDbContext : IdentityDbContext<
        ApplicationUser,
        ApplicationRole,
        int,
        IdentityUserClaim<int>,
        ApplicationUserRole,
        IdentityUserLogin<int>,
        IdentityRoleClaim<int>,
        IdentityUserToken<int>>, IApplicationDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        // Permission system
        public DbSet<UserPermission> UserPermissions { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }
        public DbSet<ChatRolePermission> ChatRolePermissions { get; set; }

        // Chat system
        public DbSet<Chat> Chats { get; set; }
        public DbSet<ChatMember> ChatMembers { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<MessageStatus> MessageStatuses { get; set; }
        public DbSet<MediaFile> MediaFiles { get; set; }

        // Audit
        public DbSet<AuditLog> AuditLogs { get; set; }

        // Relationships
        public DbSet<Friendship> Friendships { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Identity tables with custom names
            modelBuilder.Entity<ApplicationUser>(entity =>
            {
                entity.ToTable("Users");
                entity.Property(e => e.DisplayName).HasMaxLength(100).IsRequired();
                entity.Property(e => e.AvatarUrl).HasMaxLength(500);
                entity.HasIndex(e => e.Email).IsUnique();
                entity.HasIndex(e => e.UserName).IsUnique();
            });

            modelBuilder.Entity<ApplicationRole>(entity =>
            {
                entity.ToTable("Roles");
                entity.Property(e => e.Description).HasMaxLength(500);
            });

            modelBuilder.Entity<ApplicationUserRole>(entity =>
            {
                entity.ToTable("UserRoles");
                entity.HasKey(e => new { e.UserId, e.RoleId });

                entity.HasOne(e => e.User)
                    .WithMany(u => u.UserRoles)
                    .HasForeignKey(e => e.UserId);

                entity.HasOne(e => e.Role)
                    .WithMany(r => r.UserRoles)
                    .HasForeignKey(e => e.RoleId);

                entity.HasOne(e => e.AssignedByUser)
                    .WithMany()
                    .HasForeignKey(e => e.AssignedBy)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Rename default Identity tables
            modelBuilder.Entity<IdentityUserClaim<int>>().ToTable("UserClaims");
            modelBuilder.Entity<IdentityUserLogin<int>>().ToTable("UserLogins");
            modelBuilder.Entity<IdentityUserToken<int>>().ToTable("UserTokens");
            modelBuilder.Entity<IdentityRoleClaim<int>>().ToTable("RoleClaims");

            // Configure Permission System
            modelBuilder.Entity<UserPermission>(entity =>
            {
                entity.HasKey(e => e.UserId);
                entity.Property(e => e.PermissionMask).IsRequired();

                entity.HasOne(e => e.User)
                    .WithOne(u => u.UserPermission)
                    .HasForeignKey<UserPermission>(e => e.UserId);

                entity.HasOne(e => e.UpdatedByUser)
                    .WithMany()
                    .HasForeignKey(e => e.UpdatedBy)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<RolePermission>(entity =>
            {
                entity.HasKey(e => e.RoleId);
                entity.Property(e => e.PermissionMask).IsRequired();

                entity.HasOne(e => e.Role)
                    .WithOne(r => r.RolePermission)
                    .HasForeignKey<RolePermission>(e => e.RoleId);

                entity.HasOne(e => e.UpdatedByUser)
                    .WithMany()
                    .HasForeignKey(e => e.UpdatedBy)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<ChatRolePermission>(entity =>
            {
                entity.Property(e => e.PermissionMask).IsRequired();

                entity.HasKey(e => e.ChatId);
                entity.HasOne(e => e.Chat)
                    .WithMany(r => r.RolePermissions)
                    .HasForeignKey(e => e.ChatId);

                entity.HasOne(e => e.UpdatedByUser)
                    .WithMany()
                    .HasForeignKey(e => e.UpdatedBy)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Configure Chat System
            modelBuilder.Entity<Chat>(entity =>
            {
                entity.HasKey(e => e.ChatId);
                entity.Property(e => e.ChatName).HasMaxLength(100);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.AvatarUrl).HasMaxLength(500);
                entity.Property(e => e.ChatType).HasConversion<int>();

                entity.HasOne(e => e.Creator)
                    .WithMany(u => u.CreatedChats)
                    .HasForeignKey(e => e.CreatedBy)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.LastMessage)
                    .WithMany()
                    .HasForeignKey(e => e.LastMessageId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasIndex(e => e.ChatType);
                entity.HasIndex(e => e.CreatedBy);
            });

            modelBuilder.Entity<ChatMember>(entity =>
            {
                entity.HasKey(e => new { e.ChatId, e.UserId });
                entity.Property(e => e.Role).HasConversion<int>();

                entity.HasOne(e => e.Chat)
                    .WithMany(c => c.Members)
                    .HasForeignKey(e => e.ChatId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.User)
                    .WithMany(u => u.ChatMemberships)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.AddedByUser)
                    .WithMany()
                    .HasForeignKey(e => e.AddedBy)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(e => e.LastReadMessage)
                    .WithMany()
                    .HasForeignKey(e => e.LastReadMessageId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasIndex(e => e.ChatId);
                entity.HasIndex(e => e.UserId);
            });

            modelBuilder.Entity<Message>(entity =>
            {
                entity.HasKey(e => e.MessageId);
                entity.Property(e => e.Content).HasMaxLength(4000);
                entity.Property(e => e.MessageType).HasConversion<int>();

                entity.HasOne(e => e.Chat)
                    .WithMany(c => c.Messages)
                    .HasForeignKey(e => e.ChatId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Sender)
                    .WithMany(u => u.Messages)
                    .HasForeignKey(e => e.SenderId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.MediaFile)
                    .WithMany(f => f.Messages)
                    .HasForeignKey(e => e.MediaFileId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(e => e.ReplyToMessage)
                    .WithMany(m => m.Replies)
                    .HasForeignKey(e => e.ReplyToMessageId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(e => e.ThreadRootMessage)
                    .WithMany(m => m.ThreadMessages)
                    .HasForeignKey(e => e.ThreadRootMessageId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(e => e.DeletedByUser)
                    .WithMany()
                    .HasForeignKey(e => e.DeletedBy)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasIndex(e => e.ChatId);
                entity.HasIndex(e => e.SenderId);
                entity.HasIndex(e => e.CreatedAt);
                entity.HasIndex(e => new { e.ChatId, e.CreatedAt });
            });

            modelBuilder.Entity<MessageStatus>(entity =>
            {
                entity.HasKey(e => new { e.MessageId, e.UserId });
                entity.Property(e => e.Status).HasConversion<int>();

                entity.HasOne(e => e.Message)
                    .WithMany(m => m.MessageStatuses)
                    .HasForeignKey(e => e.MessageId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(e => e.MessageId);
                entity.HasIndex(e => e.UserId);
            });

            modelBuilder.Entity<MediaFile>(entity =>
            {
                entity.HasKey(e => e.FileId);
                entity.Property(e => e.FileName).HasMaxLength(255).IsRequired();
                entity.Property(e => e.OriginalFileName).HasMaxLength(255).IsRequired();
                entity.Property(e => e.ContentType).HasMaxLength(100).IsRequired();
                entity.Property(e => e.FilePath).HasMaxLength(1000).IsRequired();
                entity.Property(e => e.ThumbnailPath).HasMaxLength(1000);

                entity.HasOne(e => e.UploadedByUser)
                    .WithMany(u => u.UploadedFiles)
                    .HasForeignKey(e => e.UploadedBy)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(e => e.UploadedBy);
                entity.HasIndex(e => e.UploadedAt);
            });

            modelBuilder.Entity<AuditLog>(entity =>
            {
                entity.HasKey(e => e.LogId);
                entity.Property(e => e.Action).HasMaxLength(100).IsRequired();
                entity.Property(e => e.EntityType).HasMaxLength(100).IsRequired();
                entity.Property(e => e.IpAddress).HasMaxLength(50);
                entity.Property(e => e.UserAgent).HasMaxLength(500);

                entity.HasOne(e => e.User)
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.Timestamp);
                entity.HasIndex(e => new { e.EntityType, e.EntityId });
            });

            modelBuilder.Entity<Friendship>(entity =>
            {
                entity.HasKey(f => f.Id);

                entity.HasOne(f => f.Requester)
                      .WithMany(u => u.SentFriendRequests)
                      .HasForeignKey(f => f.RequesterId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(f => f.Receiver)
                      .WithMany(u => u.ReceivedFriendRequests)
                      .HasForeignKey(f => f.ReceiverId)
                      .OnDelete(DeleteBehavior.Restrict);

                // Prevent duplicate friend requests
                entity.HasIndex(f => new { f.RequesterId, f.ReceiverId })
                      .IsUnique();
            });
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            // Update timestamps
            var entries = ChangeTracker.Entries()
                .Where(e => e.Entity is ApplicationUser && (e.State == EntityState.Added || e.State == EntityState.Modified));

            foreach (var entry in entries)
            {
                if (entry.Entity is ApplicationUser user)
                {
                    if (entry.State == EntityState.Added)
                    {
                        user.CreatedAt = DateTime.UtcNow;
                    }
                    user.UpdatedAt = DateTime.UtcNow;
                }
            }

            return await base.SaveChangesAsync(cancellationToken);
        }
    }
}
