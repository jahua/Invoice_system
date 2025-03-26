using InvoiceSystem.Domain.Entities;

namespace InvoiceSystem.Domain.Interfaces;

public interface IInvoiceRepository
{
    Task<IEnumerable<Invoice>> GetAllAsync();
    Task<Invoice?> GetByIdAsync(int id);
    Task<IEnumerable<Invoice>> GetByEmployeeIdAsync(int employeeId);
    Task<Invoice> AddAsync(Invoice invoice);
    Task UpdateAsync(Invoice invoice);
    Task DeleteAsync(int id);
    Task<IEnumerable<Invoice>> GetInvoicesByEmployeeIdAsync(int employeeId);
} 