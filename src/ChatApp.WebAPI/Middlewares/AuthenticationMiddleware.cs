
using ChatApp.WebAPI.Services;
using Microsoft.AspNetCore.Authorization;

namespace ChatApp.WebAPI.Middleware
{
    public class AuthenticationMiddleware
    {
        private readonly RequestDelegate _next;

        public AuthenticationMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, IAuth0Service userSyncService)
        {
            // Chỉ xử lý với authenticated requests
            if (context.User.Identity?.IsAuthenticated == true)
            {
                try
                {
                    // Sync user từ Auth0 claims
                    var user = await userSyncService.SyncUserAsync(context.User);

                    // Add user info vào HttpContext để sử dụng sau
                    context.Items["CurrentUser"] = user;
                    context.Items["CurrentUserId"] = user.Id;
                }
                catch (Exception ex)
                {
                    // Log error nhưng không block request
                    // Logger có thể inject vào đây
                    Console.WriteLine($"Error syncing user: {ex.Message}");
                }
            }

            await _next(context);
        }
    }

    // Extension method để dễ register
    public static class AuthenticationMiddlewareExtensions
    {
        public static IApplicationBuilder UseAuthenticationSync(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<AuthenticationMiddleware>();
        }
    }
}