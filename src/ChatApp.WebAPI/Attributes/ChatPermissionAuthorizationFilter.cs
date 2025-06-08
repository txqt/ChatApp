using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using ChatApp.Domain.Enum;

public class ChatPermissionAuthorizationFilter : IAuthorizationFilter
{
    private readonly ChatPermissions _requiredPermission;

    public ChatPermissionAuthorizationFilter(ChatPermissions requiredPermission)
    {
        _requiredPermission = requiredPermission;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
    }
}