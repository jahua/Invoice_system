using InvoiceSystem.Domain.Entities;
using InvoiceSystem.Domain.Interfaces;
using InvoiceSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace InvoiceSystem.Infrastructure.Repositories
{
    public class InvoiceRepository : IInvoiceRepository
    {
        private readonly InvoiceSystemDbContext _context;
        private readonly ILogger<InvoiceRepository> _logger;

        public InvoiceRepository(InvoiceSystemDbContext context, ILogger<InvoiceRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<Invoice>> GetAllAsync()
        {
            try
            {
                _logger.LogInformation("Retrieving all invoices");
                var invoices = await _context.Invoices
                    .Include(i => i.Employee)
                    .Include(i => i.Contract)
                    .ToListAsync();
                _logger.LogInformation("Retrieved {Count} invoices", invoices.Count);
                return invoices;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving invoices");
                throw;
            }
        }

        public async Task<Invoice?> GetByIdAsync(int id)
        {
            try
            {
                _logger.LogInformation("Retrieving invoice with ID: {InvoiceId}", id);
                var invoice = await _context.Invoices
                    .Include(i => i.Employee)
                    .Include(i => i.Contract)
                    .FirstOrDefaultAsync(i => i.Id == id);
                _logger.LogInformation(invoice == null ? "Invoice not found" : "Invoice retrieved successfully");
                return invoice;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving invoice with ID: {InvoiceId}", id);
                throw;
            }
        }

        public async Task<IEnumerable<Invoice>> GetByEmployeeIdAsync(int employeeId)
        {
            try
            {
                _logger.LogInformation("Retrieving invoices for employee ID: {EmployeeId}", employeeId);
                var invoices = await _context.Invoices
                    .Include(i => i.Employee)
                    .Include(i => i.Contract)
                    .Where(i => i.EmployeeId == employeeId)
                    .ToListAsync();
                _logger.LogInformation("Retrieved {Count} invoices for employee ID: {EmployeeId}", invoices.Count, employeeId);
                return invoices;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving invoices for employee ID: {EmployeeId}", employeeId);
                throw;
            }
        }

        public async Task<Invoice> AddAsync(Invoice invoice)
        {
            try
            {
                _logger.LogInformation("Adding new invoice for employee ID: {EmployeeId}", invoice.EmployeeId);
                await _context.Invoices.AddAsync(invoice);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Successfully added invoice with ID: {InvoiceId}", invoice.Id);
                return invoice;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding invoice for employee ID: {EmployeeId}", invoice.EmployeeId);
                throw;
            }
        }

        public async Task UpdateAsync(Invoice invoice)
        {
            try
            {
                _logger.LogInformation("Updating invoice with ID: {InvoiceId}", invoice.Id);
                _context.Invoices.Update(invoice);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Successfully updated invoice with ID: {InvoiceId}", invoice.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating invoice with ID: {InvoiceId}", invoice.Id);
                throw;
            }
        }

        public async Task DeleteAsync(int id)
        {
            try
            {
                _logger.LogInformation("Deleting invoice with ID: {InvoiceId}", id);
                var invoice = await _context.Invoices.FindAsync(id);
                if (invoice != null)
                {
                    _context.Invoices.Remove(invoice);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Successfully deleted invoice with ID: {InvoiceId}", id);
                }
                else
                {
                    _logger.LogWarning("Invoice with ID: {InvoiceId} not found for deletion", id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting invoice with ID: {InvoiceId}", id);
                throw;
            }
        }

        public async Task<IEnumerable<Invoice>> GetInvoicesByEmployeeIdAsync(int employeeId)
        {
            try
            {
                _logger.LogInformation("Retrieving invoices for employee ID: {EmployeeId}", employeeId);
                var invoices = await _context.Invoices
                    .Include(i => i.Employee)
                    .Include(i => i.Contract)
                    .Where(i => i.EmployeeId == employeeId)
                    .ToListAsync();
                _logger.LogInformation("Retrieved {Count} invoices for employee ID: {EmployeeId}", invoices.Count, employeeId);
                return invoices;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving invoices for employee ID: {EmployeeId}", employeeId);
                throw;
            }
        }
    }
} 