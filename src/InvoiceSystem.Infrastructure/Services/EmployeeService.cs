using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using InvoiceSystem.Domain.DTOs;
using InvoiceSystem.Domain.Entities;
using InvoiceSystem.Domain.Interfaces;
using InvoiceSystem.Domain.Enums;
using InvoiceSystem.Infrastructure.Data;
using InvoiceSystem.Domain.Services;

namespace InvoiceSystem.Infrastructure.Services;

public class EmployeeService : IEmployeeService
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly ILogger<EmployeeService> _logger;
    private readonly InvoiceSystemDbContext _context;
    private readonly IContractValidationService _contractValidationService;

    public EmployeeService(
        IEmployeeRepository employeeRepository,
        ILogger<EmployeeService> logger,
        InvoiceSystemDbContext context,
        IContractValidationService contractValidationService)
    {
        _employeeRepository = employeeRepository;
        _logger = logger;
        _context = context;
        _contractValidationService = contractValidationService;
    }

    public async Task<IEnumerable<EmployeeDto>> GetAllEmployeesAsync()
    {
        var employees = await _employeeRepository.GetAllAsync();
        return employees.Select(e => new EmployeeDto
        {
            Id = e.Id,
            FirstName = e.FirstName,
            LastName = e.LastName,
            Email = e.Email,
            PhoneNumber = e.PhoneNumber,
            Department = e.Department,
            Position = e.Position,
            HireDate = e.HireDate,
            Salary = e.Salary
        });
    }

    public async Task<EmployeeDto?> GetEmployeeByIdAsync(int id)
    {
        var employee = await _employeeRepository.GetByIdAsync(id);
        if (employee == null)
            return null;

        return new EmployeeDto
        {
            Id = employee.Id,
            FirstName = employee.FirstName,
            LastName = employee.LastName,
            Email = employee.Email,
            PhoneNumber = employee.PhoneNumber,
            Department = employee.Department,
            Position = employee.Position,
            HireDate = employee.HireDate,
            Salary = employee.Salary
        };
    }

    public async Task<EmployeeDto> CreateEmployeeAsync(CreateEmployeeDto createEmployeeDto)
    {
        var employee = new Employee
        {
            FirstName = createEmployeeDto.FirstName,
            LastName = createEmployeeDto.LastName,
            Email = createEmployeeDto.Email,
            PhoneNumber = createEmployeeDto.PhoneNumber,
            Department = createEmployeeDto.Department,
            Position = createEmployeeDto.Position,
            HireDate = createEmployeeDto.HireDate,
            Salary = createEmployeeDto.Salary
        };

        await _employeeRepository.AddAsync(employee);
        return new EmployeeDto
        {
            Id = employee.Id,
            FirstName = employee.FirstName,
            LastName = employee.LastName,
            Email = employee.Email,
            PhoneNumber = employee.PhoneNumber,
            Department = employee.Department,
            Position = employee.Position,
            HireDate = employee.HireDate,
            Salary = employee.Salary
        };
    }

    public async Task<EmployeeDto> UpdateEmployeeAsync(int id, UpdateEmployeeDto updateEmployeeDto)
    {
        var employee = await _employeeRepository.GetByIdAsync(id);
        if (employee == null)
        {
            throw new KeyNotFoundException($"Employee with ID {id} not found");
        }

        employee.FirstName = updateEmployeeDto.FirstName;
        employee.LastName = updateEmployeeDto.LastName;
        employee.Email = updateEmployeeDto.Email;
        employee.PhoneNumber = updateEmployeeDto.PhoneNumber;
        employee.Department = updateEmployeeDto.Department;
        employee.Position = updateEmployeeDto.Position;
        employee.Salary = updateEmployeeDto.Salary;

        await _employeeRepository.UpdateAsync(employee);
        return new EmployeeDto
        {
            Id = employee.Id,
            FirstName = employee.FirstName,
            LastName = employee.LastName,
            Email = employee.Email,
            PhoneNumber = employee.PhoneNumber,
            Department = employee.Department,
            Position = employee.Position,
            Salary = employee.Salary,
            HireDate = employee.HireDate
        };
    }

    public async Task DeleteEmployeeAsync(int id)
    {
        var employee = await _employeeRepository.GetByIdAsync(id);
        if (employee == null)
        {
            throw new KeyNotFoundException($"Employee with ID {id} not found");
        }
        await _employeeRepository.DeleteAsync(id);
    }

    public async Task<IEnumerable<ContractDto>> GetEmployeeContractsAsync(int employeeId)
    {
        try
        {
            var employee = await _context.Employees
                .Include(e => e.Contracts)
                .FirstOrDefaultAsync(e => e.Id == employeeId);

            if (employee == null)
            {
                _logger.LogWarning("Employee with ID {EmployeeId} not found", employeeId);
                return Enumerable.Empty<ContractDto>();
            }

            return employee.Contracts.Select(c => new ContractDto
            {
                Id = c.Id,
                EmployeeId = c.EmployeeId,
                StartDate = c.StartDate,
                EndDate = c.EndDate,
                DailyRate = c.DailyRate,
                PayGrade = c.PayGrade,
                ContractType = c.ContractType
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving contracts for employee {EmployeeId}", employeeId);
            throw;
        }
    }

    public async Task<IEnumerable<ContractDto>> GetAllContractsAsync()
    {
        try
        {
            var contracts = await _context.Contracts
                .Include(c => c.Employee)
                .ToListAsync();

            return contracts.Select(c => new ContractDto
            {
                Id = c.Id,
                EmployeeId = c.EmployeeId,
                Employee = c.Employee,
                StartDate = c.StartDate,
                EndDate = c.EndDate,
                DailyRate = c.DailyRate,
                PayGrade = c.PayGrade,
                ContractType = c.ContractType
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all contracts");
            throw;
        }
    }

    public async Task<ContractDto?> GetContractByIdAsync(int id)
    {
        try
        {
            var contract = await _context.Contracts
                .Include(c => c.Employee)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (contract == null)
                return null;

            return new ContractDto
            {
                Id = contract.Id,
                EmployeeId = contract.EmployeeId,
                Employee = contract.Employee,
                StartDate = contract.StartDate,
                EndDate = contract.EndDate,
                DailyRate = contract.DailyRate,
                PayGrade = contract.PayGrade,
                ContractType = contract.ContractType
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving contract with ID: {ContractId}", id);
            throw;
        }
    }

    public async Task<ContractDto> CreateContractAsync(CreateContractDto dto)
    {
        try
        {
            var employee = await _employeeRepository.GetByIdAsync(dto.EmployeeId);
            if (employee == null)
                throw new InvalidOperationException($"Employee with ID {dto.EmployeeId} not found");

            // Validate contract period
            await _contractValidationService.ValidateContractPeriod(
                dto.StartDate.ToUniversalTime(),
                dto.EndDate.ToUniversalTime(),
                dto.EmployeeId);

            var contract = new Contract
            {
                EmployeeId = dto.EmployeeId,
                Employee = employee,
                StartDate = dto.StartDate.ToUniversalTime(),
                EndDate = dto.EndDate.ToUniversalTime(),
                DailyRate = dto.DailyRate,
                PayGrade = dto.PayGrade,
                ContractType = dto.ContractType
            };

            await _context.Contracts.AddAsync(contract);
            await _context.SaveChangesAsync();

            return new ContractDto
            {
                Id = contract.Id,
                EmployeeId = contract.EmployeeId,
                Employee = contract.Employee,
                StartDate = contract.StartDate,
                EndDate = contract.EndDate,
                DailyRate = contract.DailyRate,
                PayGrade = contract.PayGrade,
                ContractType = contract.ContractType
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating contract for employee {EmployeeId}", dto.EmployeeId);
            throw;
        }
    }

    public async Task<ContractDto> UpdateContractAsync(UpdateContractDto dto)
    {
        try
        {
            var contract = await _context.Contracts
                .Include(c => c.Employee)
                .FirstOrDefaultAsync(c => c.Id == dto.Id);

            if (contract == null)
                throw new InvalidOperationException($"Contract with ID {dto.Id} not found");

            // Validate contract period
            await _contractValidationService.ValidateContractPeriod(
                dto.StartDate.ToUniversalTime(),
                dto.EndDate.ToUniversalTime(),
                dto.EmployeeId,
                dto.Id);

            contract.EmployeeId = dto.EmployeeId;
            contract.StartDate = dto.StartDate.ToUniversalTime();
            contract.EndDate = dto.EndDate.ToUniversalTime();
            contract.DailyRate = dto.DailyRate;
            contract.PayGrade = dto.PayGrade;
            contract.ContractType = dto.ContractType;

            _context.Contracts.Update(contract);
            await _context.SaveChangesAsync();

            return new ContractDto
            {
                Id = contract.Id,
                EmployeeId = contract.EmployeeId,
                Employee = contract.Employee,
                StartDate = contract.StartDate,
                EndDate = contract.EndDate,
                DailyRate = contract.DailyRate,
                PayGrade = contract.PayGrade,
                ContractType = contract.ContractType
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating contract with ID: {ContractId}", dto.Id);
            throw;
        }
    }

    public async Task DeleteContractAsync(int id)
    {
        try
        {
            var contract = await _context.Contracts.FindAsync(id);
            if (contract == null)
                throw new InvalidOperationException($"Contract with ID {id} not found");

            _context.Contracts.Remove(contract);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting contract with ID: {ContractId}", id);
            throw;
        }
    }
} 