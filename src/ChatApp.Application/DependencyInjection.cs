using ChatApp.Application.Interfaces;
using ChatApp.Application.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatApp.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<ISystemPermissionService, SystemPermissionService>();
            services.AddScoped<IChatPermissionService, ChatPermissionService>();
            services.AddScoped<IUnifiedPermissionService, UnifiedPermissionService>();
            return services;
        }
    }
}
