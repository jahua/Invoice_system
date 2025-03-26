using System;
using System.Collections.Generic;
using System.Linq;
using InvoiceSystem.Domain.Entities;
using InvoiceSystem.Domain.Interfaces;

namespace InvoiceSystem.Infrastructure.Services
{
    public class InvoiceValidationService : IInvoiceValidationService
    {
        private static readonly DayOfWeek[] WeekendDays = new[] { DayOfWeek.Saturday, DayOfWeek.Sunday };
        
        public void ValidateInvoicePeriod(DateTime startDate, DateTime endDate, Contract contract)
        {
            if (startDate >= endDate)
            {
                throw new InvalidOperationException("Invoice start date must be before end date");
            }

            if (startDate < contract.StartDate || endDate > contract.EndDate)
            {
                throw new InvalidOperationException(
                    $"Invoice period ({startDate:d} - {endDate:d}) must be within contract period ({contract.StartDate:d} - {contract.EndDate:d})");
            }
        }

        public void ValidateNoOverlappingInvoices(DateTime startDate, DateTime endDate, IEnumerable<Invoice> existingInvoices)
        {
            foreach (var invoice in existingInvoices)
            {
                if (DoPeriodsOverlap(startDate, endDate, invoice.StartDate, invoice.EndDate))
                {
                    throw new InvalidOperationException(
                        $"Invoice period overlaps with existing invoice ({invoice.StartDate:d} - {invoice.EndDate:d})");
                }
            }
        }

        public int ValidateDaysWorked(DateTime startDate, DateTime endDate)
        {
            var totalDays = (endDate - startDate).Days + 1;
            if (totalDays <= 0)
            {
                throw new InvalidOperationException("Invalid date range for days worked calculation");
            }

            // Count only weekdays
            var workingDays = 0;
            var currentDate = startDate;
            while (currentDate <= endDate)
            {
                if (!WeekendDays.Contains(currentDate.DayOfWeek))
                {
                    workingDays++;
                }
                currentDate = currentDate.AddDays(1);
            }

            return workingDays;
        }

        public void ValidateTotalAmount(decimal totalAmount, int daysWorked, decimal dailyRate)
        {
            var expectedAmount = daysWorked * dailyRate;
            if (totalAmount != expectedAmount)
            {
                throw new InvalidOperationException(
                    $"Total amount ({totalAmount:C}) does not match expected amount ({expectedAmount:C})");
            }
        }

        private bool IsWeekend(DateTime date)
        {
            return WeekendDays.Contains(date.DayOfWeek);
        }

        private bool IsHoliday(DateTime date)
        {
            // TODO: Implement holiday checking logic
            // This could be expanded to use a holiday calendar service or database
            return false;
        }

        private bool DoPeriodsOverlap(DateTime start1, DateTime end1, DateTime start2, DateTime end2)
        {
            return start1 <= end2 && end1 >= start2;
        }
    }
} 