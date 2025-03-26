using InvoiceSystem.Domain.Entities;
using InvoiceSystem.Domain.Interfaces;
using InvoiceSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InvoiceSystem.Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly InvoiceSystemDbContext _context;

        public UserRepository(InvoiceSystemDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<User>> GetAllAsync()
        {
            return await _context.Users
                .Include(u => u.Employee)
                .ToListAsync();
        }

        public async Task<User> GetByIdAsync(int id)
        {
            return await _context.Users
                .Include(u => u.Employee)
                .FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task<User> GetByUsernameAsync(string username)
        {
            return await _context.Users
                .Include(u => u.Employee)
                .FirstOrDefaultAsync(u => u.Username == username);
        }

        public async Task AddAsync(User user)
        {
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(User user)
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(User user)
        {
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
        }
    }
} 