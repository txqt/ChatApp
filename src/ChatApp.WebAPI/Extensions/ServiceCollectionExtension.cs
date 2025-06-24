using Auth0.ManagementApi;
using ChatApp.Application;
using ChatApp.Domain.Entities;
using ChatApp.Infrastructure;
using ChatApp.Infrastructure.Data;

using ChatApp.WebAPI.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.WebAPI.Extensions
{
    public static class ServiceCollectionExtension
    {
        public static IServiceCollection AddServiceCollection(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseNpgsql(Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection"));
            });

            services.AddApplication(configuration);
            services.AddInfrastructure(configuration);

            services.AddIdentity<ApplicationUser, ApplicationRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            services.AddSignalR();

            services.AddScoped<IAuth0Service, Auth0Service>();
            services.AddScoped<IMediaService, MediaService>();

            services.AddHttpClient();
            return services;
        }
    }
}
