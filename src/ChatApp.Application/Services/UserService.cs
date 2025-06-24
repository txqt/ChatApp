using ChatApp.Contracts.DTOs;
using ChatApp.Application.Interfaces;
using ChatApp.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace ChatApp.Application.Services
{
    public class UserService : IUserService
    {
        private readonly IApplicationDbContext _db;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private ApplicationUser? _cacheUser;
        public UserService(IApplicationDbContext db, IHttpContextAccessor httpContextAccessor)
        {
            _db = db;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task EnsureUserExistsAsync(UserCreateDto dto)
        {
            var existing = await _db.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == dto.Auth0Id);

            if (existing is null)
            {
                var newUser = new ApplicationUser
                {
                    Id = dto.Auth0Id,
                    Email = dto.Email,
                    DisplayName = dto.Name,
                    CreatedAt = DateTime.UtcNow
                };
                _db.Users.Add(newUser);
                await _db.SaveChangesAsync();
            }
            else
            {
                existing.Email = dto.Email;
                existing.DisplayName = dto.Name;
                _db.Users.Update(existing);
                await _db.SaveChangesAsync();
            }
        }

        public async Task<ApplicationUser?> GetCurrentUserAsync()
        {
            if (_cacheUser is not null)
                return _cacheUser;

            var user = _httpContextAccessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated != true)
                return null;

            // Chỉ cần lấy ClaimTypes.NameIdentifier ("sub" do IUserIdProvider đã map)
            var auth0Id = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(auth0Id))
                return null;

            _cacheUser = await GetUserByIdAsync(auth0Id);
            return _cacheUser;
        }

        public async Task<ApplicationUser?> GetUserByIdAsync(string auth0Id)
        {
            return await _db.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .Include(u => u.UserPermission)
                .FirstOrDefaultAsync(u => u.Id == auth0Id);
        }
    }
}
