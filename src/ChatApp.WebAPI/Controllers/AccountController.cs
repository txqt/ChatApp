using Auth0.ManagementApi;
using Auth0.ManagementApi.Models;
using ChatApp.Application.DTOs;
using ChatApp.Infrastructure.Services;
using ChatApp.WebAPI.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;

namespace ChatApp.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController : BaseController
    {
        private readonly ManagementApiClient _mgmtClient;

        public AccountController(ManagementApiClient mgmtClient, IUserService userService) : base(userService)
        {
            _mgmtClient = mgmtClient;
        }

        [HttpPatch("update-email")]
        public async Task<IActionResult> UpdateEmail([FromBody] EmailUpdateModel model)
        {
            if (CurrentUser == null)
                return Unauthorized("You must be logged in to access chat messages");

            var request = new UserUpdateRequest
            {
                Email = model.NewEmail,
                EmailVerified = false
            };

            var updatedUser = await _mgmtClient.Users.UpdateAsync(CurrentUserId.ToString(), request);
            return Ok(new { message = "Email updated", email = updatedUser.Email });
        }
    }

}
