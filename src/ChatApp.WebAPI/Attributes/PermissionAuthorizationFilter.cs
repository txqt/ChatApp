using ChatApp.Application.Extensions;
using ChatApp.Application.Services;
using ChatApp.Domain.Entities;
using ChatApp.Domain.Enum;
using ChatApp.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

public class PermissionAuthorizationFilter : IAsyncAuthorizationFilter
{
    private readonly AppPermissions _requiredPermission;
    private readonly IUserService _userService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ISystemPermissionService _systemPermissionService;

    public PermissionAuthorizationFilter(AppPermissions requiredPermission, IUserService userService, IHttpContextAccessor httpContextAccessor, ISystemPermissionService systemPermissionService)
    {
        _requiredPermission = requiredPermission;
        _userService = userService;
        _httpContextAccessor = httpContextAccessor;
        _systemPermissionService = systemPermissionService;
    }
    public async Task<ApplicationUser> GetCurrentUser()
    {
        var auth0Id = _httpContextAccessor?.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? _httpContextAccessor?.HttpContext?.User?.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(auth0Id))
        {
            return null;
        }

        var currentUser = await _userService.GetUserByAuth0IdAsync(auth0Id);
        if (currentUser is null)
        {
            return null;
        }

        return currentUser;
    }
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;
        if (!user.Identity.IsAuthenticated)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var currentUser = await GetCurrentUser();
        if (currentUser == null)
        {
            context.Result = new UnauthorizedResult();
        }

        if(await _systemPermissionService.HasSystemPermission(currentUser.Id, _requiredPermission) == false)
        {
            context.Result = new ForbidResult();
        }
    }
}