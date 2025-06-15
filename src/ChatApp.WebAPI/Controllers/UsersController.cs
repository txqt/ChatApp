using Auth0.ManagementApi.Models;
using ChatApp.Application.DTOs;
using ChatApp.Domain.Enum;
using ChatApp.Infrastructure.Services;
using ChatApp.WebAPI.Attributes;
using ChatApp.WebAPI.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ChatApp.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : BaseController
    {
        private readonly IAuth0Service _auth0Service;
        private readonly ISystemPermissionService _systemPermissionService;
        private readonly IFriendService _friendService;
        public UsersController(IUserService userService, IAuth0Service auth0Service, ISystemPermissionService systemPermissionService, IFriendService friendService) : base(userService)
        {
            _auth0Service = auth0Service;
            _systemPermissionService = systemPermissionService;
            _friendService = friendService;
        }

        [HttpPut("{userId}")]
        public async Task<IActionResult> UpdateUser(string userId, [FromBody] UserInfoDto dto)
        {
            try
            {
                var permisison = await _systemPermissionService.GetUserPermissions(CurrentUserId);
                if (permisison.HasFlag(AppPermissions.EditUser) | (!permisison.HasFlag(AppPermissions.EditOwnProfile) && CurrentUserId.ToString() == userId))
                {
                    return Forbid("You do not have permission.");
                }

                var updateRequest = new UserUpdateRequest
                {
                    Email = dto.Email,
                    Picture = dto.Picture,
                    FullName = dto.FullName,
                };

                var updatedUser = await _auth0Service.UpdateUserAsync(userId, updateRequest);

                return Ok(new
                {
                    UserId = updatedUser.UserId,
                    Email = updatedUser.Email,
                    FullName = updatedUser.FullName,
                    Picture = updatedUser.Picture
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Error updating user: {ex.Message}");
            }
        }

        [HttpGet("me")]
        public async Task<ActionResult<UserInfoDto>> GetMe()
        {
            var user = CurrentUser;
            if (user == null)
                return NotFound();

            return Ok(new UserInfoDto
            {
                Id = user.Id,
                Email = user.Email,
                FullName = user.DisplayName,
                Picture = user.AvatarUrl,
                Role = user.UserRoles?.FirstOrDefault()?.Role?.Name ?? "User",
            });
        }

        [HttpGet("me/friends")]
        public async Task<ActionResult<List<UserInfoDto>>> GetMyFriends()
        {
            var user = CurrentUser;
            if (user == null)
                return NotFound();
            var friends = await _friendService.GetFriendsAsync(CurrentUserId);
            return Ok(friends);
        }
    }
}
