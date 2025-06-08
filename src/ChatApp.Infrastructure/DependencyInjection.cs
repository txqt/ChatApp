using ChatApp.Application.Interfaces;
using ChatApp.Infrastructure.Data;
using ChatApp.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ChatApp.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<IApplicationDbContext, ApplicationDbContext>();
            services.AddScoped<IAuth0Service, Auth0Service>();
            services.AddScoped<IUserService, UserService>();
            return services;
        }
    }
}
