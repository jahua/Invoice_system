using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InvoiceSystem.Domain.Entities;
using InvoiceSystem.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using InvoiceSystem.Infrastructure.Data;

namespace InvoiceSystem.Infrastructure.Services
{
    public class ContractValidationService : IContractValidationService
    {
        private readonly InvoiceSystemDbContext _context;

        public ContractValidationService(InvoiceSystemDbContext context)
        {
            _context = context;
        }

        public async Task ValidateContractPeriod(DateTime startDate, DateTime endDate, int employeeId, int? excludeContractId = null)
        {
            if (startDate >= endDate)
            {
                throw new InvalidOperationException("Contract start date must be before end date");
            }

            if (startDate < DateTime.UtcNow.Date)
            {
                throw new InvalidOperationException("Contract start date cannot be in the past");
            }

            // Check for overlapping contracts
            var query = _context.Contracts
                .Where(c => c.EmployeeId == employeeId);

            if (excludeContractId.HasValue)
            {
                query = query.Where(c => c.Id != excludeContractId.Value);
            }

            var overlappingContracts = await query
                .Where(c => startDate <= c.EndDate && endDate >= c.StartDate)
                .ToListAsync();

            if (overlappingContracts.Any())
            {
                var overlappingContract = overlappingContracts.First();
                throw new InvalidOperationException(
                    $"Contract period overlaps with existing contract ({overlappingContract.StartDate:d} - {overlappingContract.EndDate:d})");
            }
        }

        public bool IsDateRangeWithinContract(DateTime startDate, DateTime endDate, Contract contract)
        {
            return startDate >= contract.StartDate && endDate <= contract.EndDate;
        }

        public void ValidateDailyRate(decimal dailyRate)
        {
            if (dailyRate <= 0)
            {
                throw new InvalidOperationException("Daily rate must be greater than zero");
            }

            if (dailyRate > 10000) // Example maximum daily rate
            {
                throw new InvalidOperationException("Daily rate exceeds maximum allowed value");
            }
        }
    }
} 