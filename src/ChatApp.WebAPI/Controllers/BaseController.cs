﻿using ChatApp.Application.Interfaces;
using ChatApp.Domain.Entities;

using ChatApp.WebAPI.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ChatApp.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BaseController : ControllerBase, IAsyncActionFilter
    {
        protected readonly IUserService _userService;

        // Property protected để các controller con dùng
        protected ApplicationUser CurrentUser { get; private set; }
        protected string CurrentUserId { get; private set; }

        public BaseController(IUserService userService)
        {
            _userService = userService;
        }

        protected IActionResult HandleResult<T>(T result)
        {
            if (result == null)
                return NotFound();

            return Ok(result);
        }

        protected IActionResult HandleResult<T>(T result, string message)
        {
            if (result == null)
                return NotFound(new { message });

            return Ok(new { data = result, message });
        }

        [NonAction]
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var currentUser = HttpContextExtensions.GetCurrentUser(HttpContext);
            if (currentUser != null)
            {
                CurrentUser = currentUser;
                CurrentUserId = currentUser.Id;
            }
            else
            {
                // Nếu không có user trong context, lấy từ service
                CurrentUser = await _userService.GetCurrentUserAsync();
                CurrentUserId = CurrentUser?.Id ?? "";
            }
            await next();
        }
    }
}
