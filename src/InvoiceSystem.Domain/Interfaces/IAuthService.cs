using InvoiceSystem.Domain.Entities;

namespace InvoiceSystem.Domain.Interfaces
{
    public interface IAuthService
    {
        Task<User?> AuthenticateAsync(string username, string password);
        Task<bool> IsInvoiceManagerAsync(int userId);
        Task<bool> IsEmployeeAsync(int userId);
        Task<int?> GetEmployeeIdAsync(int userId);
        Task<User?> GetCurrentUserAsync();
        string HashPassword(string password);
    }
} 