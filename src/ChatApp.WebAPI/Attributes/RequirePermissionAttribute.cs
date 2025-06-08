using ChatApp.Domain.Enum;
using Microsoft.AspNetCore.Mvc;

namespace ChatApp.WebAPI.Attributes
{
    /// <summary>
    /// Authorization Attribute for Controllers
    /// </summary>
    public class RequirePermissionAttribute : TypeFilterAttribute
    {
        public RequirePermissionAttribute(AppPermissions permission)
            : base(typeof(PermissionAuthorizationFilter))
        {
            Arguments = new object[] { permission };
        }
    }
}
