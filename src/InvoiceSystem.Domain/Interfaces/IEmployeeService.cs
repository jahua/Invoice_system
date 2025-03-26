using System.Collections.Generic;
using System.Threading.Tasks;
using InvoiceSystem.Domain.DTOs;

namespace InvoiceSystem.Domain.Interfaces
{
    public interface IEmployeeService
    {
        Task<IEnumerable<EmployeeDto>> GetAllEmployeesAsync();
        Task<EmployeeDto?> GetEmployeeByIdAsync(int id);
        Task<EmployeeDto> CreateEmployeeAsync(CreateEmployeeDto createEmployeeDto);
        Task<EmployeeDto> UpdateEmployeeAsync(int id, UpdateEmployeeDto updateEmployeeDto);
        Task DeleteEmployeeAsync(int id);
        Task<IEnumerable<ContractDto>> GetEmployeeContractsAsync(int employeeId);
        Task<IEnumerable<ContractDto>> GetAllContractsAsync();
        Task<ContractDto?> GetContractByIdAsync(int id);
        Task<ContractDto> CreateContractAsync(CreateContractDto dto);
        Task<ContractDto> UpdateContractAsync(UpdateContractDto dto);
        Task DeleteContractAsync(int id);
    }
} 