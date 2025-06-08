using ChatApp.Domain.Enum;
using Microsoft.AspNetCore.Mvc;

namespace ChatApp.WebAPI.Attributes
{
    /// <summary>
    /// Chat Permission Attribute for Controllers
    /// </summary>
    public class RequireChatPermissionAttribute : TypeFilterAttribute
    {
        public RequireChatPermissionAttribute(ChatPermissions permission)
            : base(typeof(ChatPermissionAuthorizationFilter))
        {
            Arguments = new object[] { permission };
        }
    }
}
