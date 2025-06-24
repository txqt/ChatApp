using ChatApp.Contracts.DTOs;
using ChatApp.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatApp.Application.Interfaces
{
    public interface IUserService
    {
        public Task EnsureUserExistsAsync(UserCreateDto dto);
        Task<ApplicationUser?> GetUserByIdAsync(string auth0Id);
        Task<ApplicationUser?> GetCurrentUserAsync();
    }
}
