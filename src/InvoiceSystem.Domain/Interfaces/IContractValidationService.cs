using System;
using System.Threading.Tasks;
using InvoiceSystem.Domain.Entities;

namespace InvoiceSystem.Domain.Interfaces
{
    public interface IContractValidationService
    {
        Task ValidateContractPeriod(DateTime startDate, DateTime endDate, int employeeId, int? excludeContractId = null);
        bool IsDateRangeWithinContract(DateTime startDate, DateTime endDate, Contract contract);
        void ValidateDailyRate(decimal dailyRate);
    }
} 