using System.Security.Claims;

namespace ChatApp.BlazorApp.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        public static string GetUserId(this ClaimsPrincipal principal)
        {
            return principal.FindFirst("sub")?.Value ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }

        public static string GetEmail(this ClaimsPrincipal principal)
        {
            return principal.FindFirst(ClaimTypes.Email)?.Value
                ?? principal.FindFirst("email")?.Value;
        }

        public static string GetFullName(this ClaimsPrincipal principal)
        {
            return principal.FindFirst("name")?.Value;
        }
    }

}
