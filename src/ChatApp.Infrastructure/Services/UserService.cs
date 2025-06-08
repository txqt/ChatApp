using ChatApp.Application.DTOs;
using ChatApp.Application.Interfaces;
using ChatApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatApp.Infrastructure.Services
{
    public interface IUserService
    {
        public Task EnsureUserExistsAsync(UserCreateDto dto);
        Task<ApplicationUser?> GetUserByAuth0IdAsync(string auth0Id);
    }
    public class UserService : IUserService
    {
        private readonly IApplicationDbContext _db;
        public UserService(IApplicationDbContext db) => _db = db;
        public async Task EnsureUserExistsAsync(UserCreateDto dto)
        {
            var existing = await _db.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Auth0Id == dto.Auth0Id);

            if (existing is null)
            {
                var newUser = new ApplicationUser
                {
                    Auth0Id = dto.Auth0Id,
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

        public async Task<ApplicationUser?> GetUserByAuth0IdAsync(string auth0Id)
        {
            return await _db.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .Include(u => u.UserPermission)
                .FirstOrDefaultAsync(u => u.Auth0Id == auth0Id);
        }
    }
}
