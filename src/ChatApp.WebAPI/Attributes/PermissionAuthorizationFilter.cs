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
    private readonly ISystemPermissionService _systemPermissionService;

    public PermissionAuthorizationFilter(AppPermissions requiredPermission, IUserService userService, ISystemPermissionService systemPermissionService)
    {
        _requiredPermission = requiredPermission;
        _userService = userService;
        _systemPermissionService = systemPermissionService;
    }
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;
        if (user?.Identity == null || !user.Identity.IsAuthenticated)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var auth0Id = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? user?.FindFirst("sub")?.Value;
        if (auth0Id == null)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        if(await _systemPermissionService.CanUserPerformAction(auth0Id, _requiredPermission) == false)
        {
            context.Result = new ForbidResult();
            return;
        }
    }
}