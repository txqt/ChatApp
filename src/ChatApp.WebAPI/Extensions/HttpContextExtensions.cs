using Auth0.ManagementApi.Models;
using ChatApp.Domain.Entities;

namespace ChatApp.WebAPI.Extensions
{
    public static class HttpContextExtensions
    {
        public static ApplicationUser GetCurrentUser(this HttpContext context)
        {
            return context.Items["CurrentUser"] as ApplicationUser;
        }

        public static int? GetCurrentUserId(this HttpContext context)
        {
            return context.Items["CurrentUserId"] as int?;
        }
    }
}
