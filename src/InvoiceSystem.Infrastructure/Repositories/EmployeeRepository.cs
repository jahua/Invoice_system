using InvoiceSystem.Domain.Entities;
using InvoiceSystem.Domain.Interfaces;
using InvoiceSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace InvoiceSystem.Infrastructure.Repositories
{
    public class EmployeeRepository : IEmployeeRepository
    {
        private readonly InvoiceSystemDbContext _context;
        private readonly ILogger<EmployeeRepository> _logger;

        public EmployeeRepository(InvoiceSystemDbContext context, ILogger<EmployeeRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<Employee>> GetAllAsync()
        {
            try
            {
                _logger.LogInformation("Retrieving all employees");
                var employees = await _context.Employees.ToListAsync();
                _logger.LogInformation("Retrieved {Count} employees", employees.Count);
                return employees;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving employees");
                throw;
            }
        }

        public async Task<Employee?> GetByIdAsync(int id)
        {
            try
            {
                _logger.LogInformation("Retrieving employee with ID: {EmployeeId}", id);
                var employee = await _context.Employees.FindAsync(id);
                _logger.LogInformation(employee == null ? "Employee not found" : "Employee retrieved successfully");
                return employee;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving employee with ID: {EmployeeId}", id);
                throw;
            }
        }

        public async Task<Employee> AddAsync(Employee employee)
        {
            try
            {
                _logger.LogInformation("Adding new employee");
                // Ensure HireDate is in UTC
                if (employee.HireDate.Kind == DateTimeKind.Local)
                {
                    employee.HireDate = employee.HireDate.ToUniversalTime();
                }
                await _context.Employees.AddAsync(employee);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Successfully added employee with ID: {EmployeeId}", employee.Id);
                return employee;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding employee");
                throw;
            }
        }

        public async Task UpdateAsync(Employee employee)
        {
            try
            {
                _logger.LogInformation("Updating employee with ID: {EmployeeId}", employee.Id);
                // Ensure HireDate is in UTC
                if (employee.HireDate.Kind == DateTimeKind.Local)
                {
                    employee.HireDate = employee.HireDate.ToUniversalTime();
                }
                _context.Employees.Update(employee);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Successfully updated employee with ID: {EmployeeId}", employee.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating employee with ID: {EmployeeId}", employee.Id);
                throw;
            }
        }

        public async Task DeleteAsync(int id)
        {
            try
            {
                _logger.LogInformation("Deleting employee with ID: {EmployeeId}", id);
                var employee = await _context.Employees.FindAsync(id);
                if (employee != null)
                {
                    _context.Employees.Remove(employee);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Successfully deleted employee with ID: {EmployeeId}", id);
                }
                else
                {
                    _logger.LogWarning("Employee with ID: {EmployeeId} not found for deletion", id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting employee with ID: {EmployeeId}", id);
                throw;
            }
        }
    }
} 