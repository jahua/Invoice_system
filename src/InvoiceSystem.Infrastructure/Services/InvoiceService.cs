using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using InvoiceSystem.Domain.DTOs;
using InvoiceSystem.Domain.Entities;
using InvoiceSystem.Domain.Interfaces;
using InvoiceSystem.Domain.Enums;
using InvoiceSystem.Infrastructure.Data;
using Microsoft.Extensions.Logging;

namespace InvoiceSystem.Infrastructure.Services
{
    public class InvoiceService : IInvoiceService
    {
        private readonly IInvoiceRepository _invoiceRepository;
        private readonly IEmployeeRepository _employeeRepository;
        private readonly InvoiceSystemDbContext _context;
        private readonly ILogger<InvoiceService> _logger;
        private readonly IInvoiceValidationService _validationService;

        public InvoiceService(
            IInvoiceRepository invoiceRepository,
            IEmployeeRepository employeeRepository,
            InvoiceSystemDbContext context,
            ILogger<InvoiceService> logger,
            IInvoiceValidationService validationService)
        {
            _invoiceRepository = invoiceRepository;
            _employeeRepository = employeeRepository;
            _context = context;
            _logger = logger;
            _validationService = validationService;
        }

        public async Task<IEnumerable<InvoiceDto>> GetAllInvoicesAsync()
        {
            var invoices = await _invoiceRepository.GetAllAsync();
            var dtos = new List<InvoiceDto>();
            foreach (var invoice in invoices)
            {
                dtos.Add(await MapToDto(invoice));
            }
            return dtos;
        }

        public async Task<InvoiceDto> GetInvoiceByIdAsync(int id)
        {
            var invoice = await _invoiceRepository.GetByIdAsync(id);
            if (invoice == null)
                return null;

            return new InvoiceDto
            {
                Id = invoice.Id,
                EmployeeId = invoice.EmployeeId,
                EmployeeName = invoice.Employee != null ? $"{invoice.Employee.FirstName} {invoice.Employee.LastName}" : "",
                StartDate = invoice.StartDate,
                EndDate = invoice.EndDate,
                DaysWorked = invoice.DaysWorked,
                Status = invoice.Status,
                CreatedAt = invoice.CreatedAt
            };
        }

        public async Task<InvoiceDto> CreateInvoiceAsync(CreateInvoiceDto createInvoiceDto)
        {
            try
            {
                // Get contract and validate it exists
                var contract = await _context.Contracts
                    .Include(c => c.Employee)
                    .FirstOrDefaultAsync(c => c.Id == createInvoiceDto.ContractId && c.EmployeeId == createInvoiceDto.EmployeeId);
                if (contract == null)
                {
                    throw new InvalidOperationException($"Contract with ID {createInvoiceDto.ContractId} not found or does not belong to employee {createInvoiceDto.EmployeeId}");
                }

                // Validate invoice period against contract period
                _validationService.ValidateInvoicePeriod(
                    createInvoiceDto.StartDate.ToUniversalTime(), 
                    createInvoiceDto.EndDate.ToUniversalTime(), 
                    contract);

                // Get existing invoices and validate no overlaps
                var existingInvoices = await _invoiceRepository.GetByEmployeeIdAsync(createInvoiceDto.EmployeeId);
                _validationService.ValidateNoOverlappingInvoices(
                    createInvoiceDto.StartDate.ToUniversalTime(),
                    createInvoiceDto.EndDate.ToUniversalTime(),
                    existingInvoices);

                // Calculate and validate working days
                var calculatedDays = _validationService.ValidateDaysWorked(
                    createInvoiceDto.StartDate.ToUniversalTime(),
                    createInvoiceDto.EndDate.ToUniversalTime());
                
                if (createInvoiceDto.DaysWorked > calculatedDays)
                {
                    throw new InvalidOperationException(
                        $"Days worked ({createInvoiceDto.DaysWorked}) cannot exceed actual working days ({calculatedDays})");
                }

                // Calculate total amount
                var totalAmount = createInvoiceDto.DaysWorked * contract.DailyRate;
                _validationService.ValidateTotalAmount(totalAmount, createInvoiceDto.DaysWorked, contract.DailyRate);

                var invoice = new Invoice
                {
                    EmployeeId = createInvoiceDto.EmployeeId,
                    ContractId = createInvoiceDto.ContractId,
                    StartDate = createInvoiceDto.StartDate.ToUniversalTime(),
                    EndDate = createInvoiceDto.EndDate.ToUniversalTime(),
                    DaysWorked = createInvoiceDto.DaysWorked,
                    TotalAmount = totalAmount,
                    Status = InvoiceStatus.Draft,
                    InvoiceNumber = await GenerateInvoiceNumberAsync(),
                    CreatedAt = DateTime.UtcNow
                };

                var createdInvoice = await _invoiceRepository.AddAsync(invoice);
                return await MapToDto(createdInvoice);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating invoice for employee {EmployeeId}", createInvoiceDto.EmployeeId);
                throw;
            }
        }

        public async Task<InvoiceDto> UpdateInvoiceAsync(UpdateInvoiceDto updateDto)
        {
            var invoice = await _invoiceRepository.GetByIdAsync(updateDto.Id);
            if (invoice == null)
            {
                throw new InvalidOperationException($"Invoice with ID {updateDto.Id} not found");
            }

            var contract = await _context.Contracts.FirstOrDefaultAsync(c => c.Id == invoice.ContractId);
            if (contract == null)
            {
                throw new InvalidOperationException($"Contract with ID {invoice.ContractId} not found");
            }

            // Validate invoice period against contract period
            _validationService.ValidateInvoicePeriod(
                updateDto.StartDate,
                updateDto.EndDate,
                contract);

            // Get existing invoices and validate no overlaps (excluding current invoice)
            var existingInvoices = await _invoiceRepository.GetByEmployeeIdAsync(invoice.EmployeeId);
            var filteredInvoices = existingInvoices.Where(i => i.Id != updateDto.Id);
            _validationService.ValidateNoOverlappingInvoices(
                updateDto.StartDate,
                updateDto.EndDate,
                filteredInvoices);

            // Calculate and validate working days
            var calculatedDays = _validationService.ValidateDaysWorked(
                updateDto.StartDate,
                updateDto.EndDate);

            if (updateDto.DaysWorked != calculatedDays)
            {
                throw new InvalidOperationException(
                    $"Days worked ({updateDto.DaysWorked}) does not match actual working days ({calculatedDays})");
            }

            // Calculate and validate total amount
            var totalAmount = calculatedDays * contract.DailyRate;
            _validationService.ValidateTotalAmount(totalAmount, calculatedDays, contract.DailyRate);

            invoice.StartDate = updateDto.StartDate.ToUniversalTime();
            invoice.EndDate = updateDto.EndDate.ToUniversalTime();
            invoice.DaysWorked = calculatedDays;
            invoice.TotalAmount = totalAmount;
            invoice.Status = updateDto.Status;

            await _invoiceRepository.UpdateAsync(invoice);
            return await MapToDto(invoice);
        }

        public async Task<bool> DeleteInvoiceAsync(int id)
        {
            var invoice = await _invoiceRepository.GetByIdAsync(id);
            if (invoice == null)
                return false;

            await _invoiceRepository.DeleteAsync(id);
            return true;
        }

        public async Task<IEnumerable<InvoiceDto>> GetEmployeeInvoicesAsync(int employeeId)
        {
            var invoices = await _invoiceRepository.GetByEmployeeIdAsync(employeeId);
            var dtos = new List<InvoiceDto>();
            foreach (var invoice in invoices)
            {
                dtos.Add(await MapToDto(invoice));
            }
            return dtos;
        }

        public async Task<IEnumerable<InvoiceDto>> GetInvoicesByEmployeeIdAsync(int employeeId)
        {
            var invoices = await _invoiceRepository.GetInvoicesByEmployeeIdAsync(employeeId);
            return invoices.Select(i => new InvoiceDto
            {
                Id = i.Id,
                EmployeeId = i.EmployeeId,
                EmployeeName = i.Employee != null ? $"{i.Employee.FirstName} {i.Employee.LastName}" : "",
                StartDate = i.StartDate,
                EndDate = i.EndDate,
                DaysWorked = i.DaysWorked,
                Status = i.Status,
                CreatedAt = i.CreatedAt
            });
        }

        private async Task<InvoiceDto> MapToDto(Invoice invoice)
        {
            var employee = await _employeeRepository.GetByIdAsync(invoice.EmployeeId);
            var contract = await _context.Contracts.FirstOrDefaultAsync(c => c.Id == invoice.ContractId);

            if (employee == null || contract == null)
            {
                throw new InvalidOperationException("Employee or Contract not found");
            }

            return new InvoiceDto
            {
                Id = invoice.Id,
                InvoiceNumber = invoice.InvoiceNumber,
                EmployeeId = invoice.EmployeeId,
                Employee = employee,
                ContractId = invoice.ContractId,
                Contract = contract,
                EmployeeName = $"{employee.FirstName} {employee.LastName}",
                ContractType = contract.ContractType.ToString(),
                PayGrade = contract.PayGrade.ToString(),
                StartDate = invoice.StartDate,
                EndDate = invoice.EndDate,
                DaysWorked = invoice.DaysWorked,
                TotalAmount = invoice.TotalAmount,
                Status = invoice.Status,
                CreatedAt = invoice.CreatedAt
            };
        }

        private async Task<string> GenerateInvoiceNumberAsync()
        {
            var today = DateTime.UtcNow.Date;
            var invoicesForToday = await _context.Invoices
                .Where(i => i.CreatedAt.Date == today)
                .ToListAsync();
            var sequenceNumber = invoicesForToday.Count + 1;
            return $"INV-{today:yyyyMMdd}-{sequenceNumber:D4}";
        }
    }
} 