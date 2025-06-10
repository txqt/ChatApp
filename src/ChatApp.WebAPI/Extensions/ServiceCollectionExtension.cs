using ChatApp.Domain.Entities;
using ChatApp.Infrastructure;
using ChatApp.Infrastructure.Data;
using ChatApp.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ChatApp.Application;
using ChatApp.WebAPI.Services;
using Auth0.ManagementApi;

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

            services.AddHttpClient("Auth0", client =>
            {
                client.BaseAddress = new Uri(Environment.GetEnvironmentVariable("Auth0__Authority")); // API URL
            });

            services.AddScoped(sp =>
                sp.GetRequiredService<IHttpClientFactory>().CreateClient("Auth0"));

            services.AddSingleton(sp =>
            {
                var domain = Environment.GetEnvironmentVariable("Auth0__Authority");
                var tokenService = sp.GetRequiredService<Auth0Service>();
                var token = tokenService.GetManagementTokenAsync().Result;

                return new ManagementApiClient(token, new Uri($"https://{domain}/api/v2"));
            });
            return services;
        }
    }
}
