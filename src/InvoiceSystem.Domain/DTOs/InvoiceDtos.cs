using System;
using System.ComponentModel.DataAnnotations;
using InvoiceSystem.Domain.Enums;
using InvoiceSystem.Domain.Entities;

namespace InvoiceSystem.Domain.DTOs
{
    public class InvoiceDto
    {
        public int Id { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public int EmployeeId { get; set; }
        public Employee? Employee { get; set; }
        public int ContractId { get; set; }
        public Contract? Contract { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string ContractType { get; set; } = string.Empty;
        public string PayGrade { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int DaysWorked { get; set; }
        public decimal TotalAmount { get; set; }
        public InvoiceStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateInvoiceDto
    {
        [Required]
        public int EmployeeId { get; set; }

        [Required]
        public int ContractId { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }

        [Required]
        [Range(1, 366, ErrorMessage = "Days worked must be greater than 0")]
        public int DaysWorked { get; set; }
    }

    public class UpdateInvoiceDto
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public int ContractId { get; set; }
        public string ContractType { get; set; } = string.Empty;
        public string PayGrade { get; set; } = string.Empty;
        public decimal DailyRate { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int DaysWorked { get; set; }
        public decimal TotalAmount { get; set; }
        public InvoiceStatus Status { get; set; }
    }
} 