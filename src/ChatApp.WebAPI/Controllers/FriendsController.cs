using ChatApp.Application.DTOs;
using ChatApp.Application.Interfaces;
using ChatApp.Infrastructure.Data;
using ChatApp.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ChatApp.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Assuming you're using JWT authentication
    public class FriendsController : BaseController
    {
        private readonly IFriendService _friendService;
        private readonly ILogger<FriendsController> _logger;
        private readonly ApplicationDbContext _dbContext;

        public FriendsController(IFriendService friendService, ILogger<FriendsController> logger, IUserService userService, ApplicationDbContext dbContext) : base(userService)
        {
            _friendService = friendService;
            _logger = logger;
            _dbContext = dbContext;
        }
        [HttpGet("search")]
        public async Task<ActionResult<List<UserSearchDto>>> SearchUsers([FromQuery] string searchTerm, [FromQuery] int pageSize = 20, [FromQuery] int page = 1)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(searchTerm))
                    return BadRequest("Search term cannot be empty");

                if (CurrentUser == null)
                    return Unauthorized();

                var users = await _friendService.SearchUsersAsync(searchTerm, CurrentUserId, pageSize, page);
                return Ok(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching users");
                return StatusCode(500, "An error occurred while searching users");
            }
        }

        [HttpPost("send-request")]
        public async Task<ActionResult> SendFriendRequest([FromBody] SendFriendRequestDto request)
        {
            try
            {
                if (CurrentUser == null)
                    return Unauthorized();

                if (request.ReceiverId == null)
                    return BadRequest("Receiver ID is required");

                foreach (var e in _dbContext.ChangeTracker.Entries())
                    Console.WriteLine($"{e.Entity.GetType().Name}: {e.State}");

                var success = await _friendService.SendFriendRequestAsync(CurrentUserId, request.ReceiverId);

                if (!success)
                    return BadRequest("Unable to send friend request. User may not exist or request already exists.");

                return Ok(new { message = "Friend request sent successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending friend request");
                return StatusCode(500, "An error occurred while sending friend request");
            }
        }

        [HttpPost("accept-request/{friendshipId}")]
        public async Task<ActionResult> AcceptFriendRequest(string friendshipId)
        {
            try
            {
                var currentUser = await _userService.GetCurrentUserAsync();
                if (currentUser == null)
                    return Unauthorized();

                var success = await _friendService.AcceptFriendRequestAsync(friendshipId, currentUser.Id);

                if (!success)
                    return BadRequest("Unable to accept friend request. Request may not exist or you're not authorized.");

                return Ok(new { message = "Friend request accepted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error accepting friend request");
                return StatusCode(500, "An error occurred while accepting friend request");
            }
        }

        [HttpPost("decline-request/{friendshipId}")]
        public async Task<ActionResult> DeclineFriendRequest(string friendshipId)
        {
            try
            {
                var currentUser = await _userService.GetCurrentUserAsync();
                if (currentUser == null)
                    return Unauthorized();

                var success = await _friendService.DeclineFriendRequestAsync(friendshipId, currentUser.Id);

                if (!success)
                    return BadRequest("Unable to decline friend request. Request may not exist or you're not authorized.");

                return Ok(new { message = "Friend request declined successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error declining friend request");
                return StatusCode(500, "An error occurred while declining friend request");
            }
        }

        [HttpDelete("remove/{friendId}")]
        public async Task<ActionResult> RemoveFriend(string friendId)
        {
            try
            {
                var currentUser = await _userService.GetCurrentUserAsync();
                if (currentUser == null)
                    return Unauthorized();

                var success = await _friendService.RemoveFriendAsync(friendId, currentUser.Id);

                if (!success)
                    return BadRequest("Unable to remove friend. Friend relationship may not exist.");

                return Ok(new { message = "Friend removed successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing friend");
                return StatusCode(500, "An error occurred while removing friend");
            }
        }

        [HttpGet("requests/pending")]
        public async Task<ActionResult<List<FriendRequestDto>>> GetPendingRequests()
        {
            try
            {
                var currentUser = await _userService.GetCurrentUserAsync();
                if (currentUser == null)
                    return Unauthorized();

                var requests = await _friendService.GetPendingFriendRequestsAsync(currentUser.Id);
                return Ok(requests);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pending friend requests");
                return StatusCode(500, "An error occurred while getting pending friend requests");
            }
        }

        [HttpGet("requests/sent")]
        public async Task<ActionResult<List<FriendRequestDto>>> GetSentRequests()
        {
            try
            {
                var currentUser = await _userService.GetCurrentUserAsync();
                if (currentUser == null)
                    return Unauthorized();

                var requests = await _friendService.GetSentFriendRequestsAsync(currentUser.Id);
                return Ok(requests);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sent friend requests");
                return StatusCode(500, "An error occurred while getting sent friend requests");
            }
        }

        [HttpGet("list")]
        public async Task<ActionResult<List<FriendDto>>> GetFriends()
        {
            try
            {
                var currentUser = await _userService.GetCurrentUserAsync();
                if (currentUser == null)
                    return Unauthorized();

                var friends = await _friendService.GetFriendsAsync(currentUser.Id);
                return Ok(friends);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting friends list");
                return StatusCode(500, "An error occurred while getting friends list");
            }
        }

        [HttpPost("block/{userId}")]
        public async Task<ActionResult> BlockUser(string userId)
        {
            try
            {
                var currentUser = await _userService.GetCurrentUserAsync();
                if (currentUser == null)
                    return Unauthorized();

                var success = await _friendService.BlockUserAsync(currentUser.Id, userId);

                if (!success)
                    return BadRequest("Unable to block user.");

                return Ok(new { message = "User blocked successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error blocking user");
                return StatusCode(500, "An error occurred while blocking user");
            }
        }

        [HttpPost("unblock/{userId}")]
        public async Task<ActionResult> UnblockUser(string userId)
        {
            try
            {
                var currentUser = await _userService.GetCurrentUserAsync();
                if (currentUser == null)
                    return Unauthorized();

                var success = await _friendService.UnblockUserAsync(currentUser.Id, userId);

                if (!success)
                    return BadRequest("Unable to unblock user.");

                return Ok(new { message = "User unblocked successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unblocking user");
                return StatusCode(500, "An error occurred while unblocking user");
            }
        }
    }
}
