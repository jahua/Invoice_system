using System;
using System.Collections.Generic;
using InvoiceSystem.Domain.Entities;

namespace InvoiceSystem.Domain.Services
{
    public interface IInvoiceValidationService
    {
        void ValidateInvoicePeriod(DateTime startDate, DateTime endDate, Contract contract);
        void ValidateNoOverlappingInvoices(DateTime startDate, DateTime endDate, IEnumerable<Invoice> existingInvoices);
        int ValidateDaysWorked(DateTime startDate, DateTime endDate);
        void ValidateTotalAmount(decimal totalAmount, int daysWorked, decimal dailyRate);
    }
} 