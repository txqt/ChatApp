using ChatApp.Contracts.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatApp.Application.Interfaces
{
    public interface IFriendService
    {
        Task<List<UserSearchDto>> SearchUsersAsync(string searchTerm, string currentUserId, int pageSize = 20, int page = 1);
        Task<bool> SendFriendRequestAsync(string requesterId, string receiverId);
        Task<bool> AcceptFriendRequestAsync(string friendshipId, string currentUserId);
        Task<bool> DeclineFriendRequestAsync(string friendshipId, string currentUserId);
        Task<bool> RemoveFriendAsync(string friendId, string currentUserId);
        Task<List<FriendRequestDto>> GetPendingFriendRequestsAsync(string userId);
        Task<List<FriendRequestDto>> GetSentFriendRequestsAsync(string userId);
        Task<List<FriendDto>> GetFriendsAsync(string userId);
        Task<bool> BlockUserAsync(string blockerId, string blockedId);
        Task<bool> UnblockUserAsync(string blockerId, string blockedId);
    }
}
