using System.Security.Cryptography;
using System.Text;
using InvoiceSystem.Domain.Interfaces;
using InvoiceSystem.Domain.Entities;
using InvoiceSystem.Domain.Enums;
using InvoiceSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using BCrypt.Net;

namespace InvoiceSystem.Infrastructure.Services
{
    public class AuthService : IAuthService
    {
        private readonly InvoiceSystemDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuthService(InvoiceSystemDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<User?> AuthenticateAsync(string username, string password)
        {
            var user = await _context.Users
                .Include(u => u.Employee)
                .FirstOrDefaultAsync(u => u.Username == username);

            if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
                return null;

            return user;
        }

        public string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        public async Task<bool> IsInvoiceManagerAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            return user?.Role == "InvoiceManager";
        }

        public async Task<bool> IsEmployeeAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            return user?.Role == "Employee";
        }

        public async Task<int?> GetEmployeeIdAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            return user?.EmployeeId;
        }

        public async Task<User?> GetCurrentUserAsync()
        {
            var username = _httpContextAccessor.HttpContext?.User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
                return null;

            return await _context.Users
                .Include(u => u.Employee)
                .FirstOrDefaultAsync(u => u.Username == username);
        }
    }
} 