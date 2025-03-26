using System;
using System.Collections.Generic;
using Xunit;
using InvoiceSystem.Domain.Entities;
using InvoiceSystem.Infrastructure.Services;
using InvoiceSystem.Domain.Enums;

namespace InvoiceSystem.Tests.Services
{
    public class InvoiceValidationServiceTests
    {
        private readonly InvoiceValidationService _validationService;
        private readonly Employee _testEmployee;

        public InvoiceValidationServiceTests()
        {
            _validationService = new InvoiceValidationService();
            _testEmployee = new Employee
            {
                Id = 1,
                FirstName = "Test",
                LastName = "User",
                Email = "test@example.com",
                PhoneNumber = "123-456-7890",
                Department = "Engineering",
                Position = "Software Engineer",
                Salary = 100000m,
                HireDate = new DateTime(2022, 1, 1)
            };
        }

        [Fact]
        public void ValidateInvoicePeriod_WhenPeriodWithinContract_ShouldNotThrow()
        {
            // Arrange
            var contract = new Contract
            {
                Id = 1,
                EmployeeId = _testEmployee.Id,
                Employee = _testEmployee,
                StartDate = new DateTime(2022, 3, 24),
                EndDate = new DateTime(2024, 3, 24),
                DailyRate = 250m,
                PayGrade = PayGrade.Junior,
                ContractType = ContractType.FullTime
            };
            var invoiceStartDate = new DateTime(2023, 12, 1);
            var invoiceEndDate = new DateTime(2023, 12, 31);

            // Act & Assert
            var exception = Record.Exception(() => 
                _validationService.ValidateInvoicePeriod(invoiceStartDate, invoiceEndDate, contract));
            Assert.Null(exception);
        }

        [Fact]
        public void ValidateInvoicePeriod_WhenPeriodOutsideContract_ShouldThrow()
        {
            // Arrange
            var contract = new Contract
            {
                Id = 1,
                EmployeeId = _testEmployee.Id,
                Employee = _testEmployee,
                StartDate = new DateTime(2022, 3, 24),
                EndDate = new DateTime(2024, 3, 24),
                DailyRate = 250m,
                PayGrade = PayGrade.Junior,
                ContractType = ContractType.FullTime
            };
            var invoiceStartDate = new DateTime(2024, 12, 25);
            var invoiceEndDate = new DateTime(2024, 12, 30);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => 
                _validationService.ValidateInvoicePeriod(invoiceStartDate, invoiceEndDate, contract));
            Assert.Contains("must be within contract period", exception.Message);
        }

        [Fact]
        public void ValidateNoOverlappingInvoices_WhenNoOverlap_ShouldNotThrow()
        {
            // Arrange
            var existingInvoices = new List<Invoice>
            {
                new Invoice { StartDate = new DateTime(2023, 1, 1), EndDate = new DateTime(2023, 1, 31) }
            };
            var newStartDate = new DateTime(2023, 2, 1);
            var newEndDate = new DateTime(2023, 2, 28);

            // Act & Assert
            var exception = Record.Exception(() => 
                _validationService.ValidateNoOverlappingInvoices(newStartDate, newEndDate, existingInvoices));
            Assert.Null(exception);
        }

        [Fact]
        public void ValidateNoOverlappingInvoices_WhenOverlap_ShouldThrow()
        {
            // Arrange
            var existingInvoices = new List<Invoice>
            {
                new Invoice { StartDate = new DateTime(2023, 1, 1), EndDate = new DateTime(2023, 1, 31) }
            };
            var newStartDate = new DateTime(2023, 1, 15);
            var newEndDate = new DateTime(2023, 2, 15);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => 
                _validationService.ValidateNoOverlappingInvoices(newStartDate, newEndDate, existingInvoices));
            Assert.Contains("overlaps with existing invoice", exception.Message);
        }

        [Fact]
        public void ValidateDaysWorked_WhenValidRange_ShouldReturnCorrectDays()
        {
            // Arrange
            var startDate = new DateTime(2023, 1, 1);
            var endDate = new DateTime(2023, 1, 31);

            // Act
            var daysWorked = _validationService.ValidateDaysWorked(startDate, endDate);

            // Assert
            Assert.Equal(31, daysWorked);
        }

        [Fact]
        public void ValidateDaysWorked_WhenInvalidRange_ShouldThrow()
        {
            // Arrange
            var startDate = new DateTime(2023, 1, 31);
            var endDate = new DateTime(2023, 1, 1);

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => 
                _validationService.ValidateDaysWorked(startDate, endDate));
            Assert.Contains("Invalid date range", exception.Message);
        }

        [Fact]
        public void ValidateTotalAmount_WhenCorrect_ShouldNotThrow()
        {
            // Arrange
            var daysWorked = 20;
            var dailyRate = 100m;
            var totalAmount = 2000m;

            // Act & Assert
            var exception = Record.Exception(() => 
                _validationService.ValidateTotalAmount(totalAmount, daysWorked, dailyRate));
            Assert.Null(exception);
        }

        [Fact]
        public void ValidateTotalAmount_WhenIncorrect_ShouldThrow()
        {
            // Arrange
            var daysWorked = 20;
            var dailyRate = 100m;
            var totalAmount = 2500m;

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() => 
                _validationService.ValidateTotalAmount(totalAmount, daysWorked, dailyRate));
            Assert.Contains("does not match expected amount", exception.Message);
        }
    }
} 