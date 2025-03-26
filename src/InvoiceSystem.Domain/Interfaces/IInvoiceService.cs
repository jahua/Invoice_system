using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using InvoiceSystem.Domain.DTOs;
using InvoiceSystem.Domain.Entities;

namespace InvoiceSystem.Domain.Interfaces
{
    public interface IInvoiceService
    {
        Task<InvoiceDto> CreateInvoiceAsync(CreateInvoiceDto createInvoiceDto);
        Task<InvoiceDto> GetInvoiceByIdAsync(int id);
        Task<IEnumerable<InvoiceDto>> GetAllInvoicesAsync();
        Task<IEnumerable<InvoiceDto>> GetEmployeeInvoicesAsync(int employeeId);
        Task<InvoiceDto> UpdateInvoiceAsync(UpdateInvoiceDto updateDto);
        Task<bool> DeleteInvoiceAsync(int id);
        Task<IEnumerable<InvoiceDto>> GetInvoicesByEmployeeIdAsync(int employeeId);
    }
} 