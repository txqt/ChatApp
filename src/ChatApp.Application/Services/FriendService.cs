using ChatApp.Application.Interfaces;
using ChatApp.Contracts.DTOs;
using ChatApp.Domain.Entities;
using ChatApp.Domain.Enum;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatApp.Application.Services
{
    // Services/FriendService.cs
    public class FriendService : IFriendService
    {
        private readonly IApplicationDbContext _context;
        private readonly ILogger<FriendService> _logger;

        public FriendService(IApplicationDbContext context, ILogger<FriendService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<UserSearchDto>> SearchUsersAsync(string searchTerm, string currentUserId, int pageSize = 20, int page = 1)
        {
            try
            {
                var query = _context.Users
                    .Where(u => u.Id != currentUserId &&
                               (u.UserName.Contains(searchTerm) ||
                                u.DisplayName.Contains(searchTerm) ||
                                u.Email.Contains(searchTerm)))
                    .OrderBy(u => u.DisplayName)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize);

                var users = await query.ToListAsync();
                var userIds = users.Select(u => u.Id).ToList();

                // Get friendship status for each user
                var friendships = await _context.Friendships
                    .Where(f => (f.RequesterId == currentUserId && userIds.Contains(f.ReceiverId)) ||
                               (f.ReceiverId == currentUserId && userIds.Contains(f.RequesterId)))
                    .ToListAsync();

                return users.Select(user =>
                {
                    var friendship = friendships.FirstOrDefault(f =>
                        (f.RequesterId == currentUserId && f.ReceiverId == user.Id) ||
                        (f.ReceiverId == currentUserId && f.RequesterId == user.Id));

                    return new UserSearchDto
                    {
                        Id = user.Id,
                        UserName = user.UserName,
                        DisplayName = user.DisplayName,
                        Email = user.Email,
                        Avatar = user.AvatarUrl,
                        IsOnline = user.IsOnline,
                        LastSeen = user.LastSeenAt,
                        FriendshipStatus = friendship?.Status,
                        IsFriend = friendship?.Status == FriendshipStatus.Accepted,
                        HasPendingRequest = friendship?.Status == FriendshipStatus.Pending
                    };
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching users with term: {SearchTerm}", searchTerm);
                throw;
            }
        }

        public async Task<bool> SendFriendRequestAsync(string requesterId, string receiverId)
        {
            try
            {
                if (requesterId == receiverId)
                    return false;

                // Check if friendship already exists
                var existingFriendship = await _context.Friendships
                    .FirstOrDefaultAsync(f =>
                        (f.RequesterId == requesterId && f.ReceiverId == receiverId) ||
                        (f.RequesterId == receiverId && f.ReceiverId == requesterId));

                if (existingFriendship != null)
                    return false;

                // Check if receiver exists
                var receiverExists = await _context.Users.AnyAsync(u => u.Id == receiverId);
                if (!receiverExists)
                    return false;
                

                var friendship = new Friendship
                {
                    RequesterId = requesterId,
                    ReceiverId = receiverId,
                    Status = FriendshipStatus.Pending,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Friendships.Add(friendship);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending friend request from {RequesterId} to {ReceiverId}", requesterId, receiverId);
                return false;
            }
        }

        public async Task<bool> AcceptFriendRequestAsync(string friendshipId, string  currentUserId)
        {
            try
            {
                var friendship = await _context.Friendships
                    .FirstOrDefaultAsync(f => f.Id == friendshipId && f.ReceiverId == currentUserId && f.Status == FriendshipStatus.Pending);

                if (friendship == null)
                    return false;

                friendship.Status = FriendshipStatus.Accepted;
                friendship.AcceptedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error accepting friend request {FriendshipId}", friendshipId);
                return false;
            }
        }

        public async Task<bool> DeclineFriendRequestAsync(string friendshipId, string currentUserId)
        {
            try
            {
                var friendship = await _context.Friendships
                    .FirstOrDefaultAsync(f => f.Id == friendshipId && f.ReceiverId == currentUserId && f.Status == FriendshipStatus.Pending);

                if (friendship == null)
                    return false;

                friendship.Status = FriendshipStatus.Declined;
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error declining friend request {FriendshipId}", friendshipId);
                return false;
            }
        }

        public async Task<bool> RemoveFriendAsync(string friendId, string currentUserId)
        {
            try
            {
                var friendship = await _context.Friendships
                    .FirstOrDefaultAsync(f =>
                        ((f.RequesterId == currentUserId && f.ReceiverId == friendId) ||
                         (f.RequesterId == friendId && f.ReceiverId == currentUserId)) &&
                        f.Status == FriendshipStatus.Accepted);

                if (friendship == null)
                    return false;

                _context.Friendships.Remove(friendship);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing friend {FriendId} for user {CurrentUserId}", friendId, currentUserId);
                return false;
            }
        }

        public async Task<List<FriendRequestDto>> GetPendingFriendRequestsAsync(string userId)
        {
            try
            {
                return await _context.Friendships
                    .Include(f => f.Requester)
                    .Where(f => f.ReceiverId == userId && f.Status == FriendshipStatus.Pending)
                    .OrderByDescending(f => f.CreatedAt)
                    .Select(f => new FriendRequestDto
                    {
                        Id = f.Id,
                        RequesterId = f.RequesterId,
                        RequesterName = f.Requester.UserName,
                        RequesterDisplayName = f.Requester.DisplayName,
                        RequesterAvatar = f.Requester.AvatarUrl,
                        ReceiverId = f.ReceiverId,
                        Status = f.Status,
                        CreatedAt = f.CreatedAt
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pending friend requests for user {UserId}", userId);
                throw;
            }
        }

        public async Task<List<FriendRequestDto>> GetSentFriendRequestsAsync(string  userId)
        {
            try
            {
                return await _context.Friendships
                    .Include(f => f.Receiver)
                    .Where(f => f.RequesterId == userId && f.Status == FriendshipStatus.Pending)
                    .OrderByDescending(f => f.CreatedAt)
                    .Select(f => new FriendRequestDto
                    {
                        Id = f.Id,
                        RequesterId = f.RequesterId,
                        RequesterName = f.Receiver.UserName,
                        RequesterDisplayName = f.Receiver.DisplayName,
                        RequesterAvatar = f.Receiver.AvatarUrl,
                        ReceiverId = f.ReceiverId,
                        Status = f.Status,
                        CreatedAt = f.CreatedAt
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sent friend requests for user {UserId}", userId);
                throw;
            }
        }

        public async Task<List<FriendDto>> GetFriendsAsync(string  userId)
        {
            try
            {
                var friendships = await _context.Friendships
                    .Include(f => f.Requester)
                    .Include(f => f.Receiver)
                    .Where(f => (f.RequesterId == userId || f.ReceiverId == userId) && f.Status == FriendshipStatus.Accepted)
                    .OrderByDescending(f => f.AcceptedAt)
                    .ToListAsync();

                return friendships.Select(f =>
                {
                    var friend = f.RequesterId == userId ? f.Receiver : f.Requester;
                    return new FriendDto
                    {
                        Id = friend.Id,
                        UserName = friend.UserName,
                        DisplayName = friend.DisplayName,
                        Avatar = friend.AvatarUrl,
                        IsOnline = friend.IsOnline,
                        LastSeen = friend.LastSeenAt,
                        FriendsSince = f.AcceptedAt ?? f.CreatedAt
                    };
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting friends for user {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> BlockUserAsync(string  blockerId, string  blockedId)
        {
            try
            {
                // Remove existing friendship if any
                var existingFriendship = await _context.Friendships
                    .FirstOrDefaultAsync(f =>
                        (f.RequesterId == blockerId && f.ReceiverId == blockedId) ||
                        (f.RequesterId == blockedId && f.ReceiverId == blockerId));

                if (existingFriendship != null)
                {
                    _context.Friendships.Remove(existingFriendship);
                }

                // Add block relationship
                var blockRelationship = new Friendship
                {
                    RequesterId = blockerId,
                    ReceiverId = blockedId,
                    Status = FriendshipStatus.Blocked,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Friendships.Add(blockRelationship);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error blocking user {BlockedId} by {BlockerId}", blockedId, blockerId);
                return false;
            }
        }

        public async Task<bool> UnblockUserAsync(string  blockerId, string  blockedId)
        {
            try
            {
                var blockRelationship = await _context.Friendships
                    .FirstOrDefaultAsync(f => f.RequesterId == blockerId && f.ReceiverId == blockedId && f.Status == FriendshipStatus.Blocked);

                if (blockRelationship == null)
                    return false;

                _context.Friendships.Remove(blockRelationship);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unblocking user {BlockedId} by {BlockerId}", blockedId, blockerId);
                return false;
            }
        }
    }
}
