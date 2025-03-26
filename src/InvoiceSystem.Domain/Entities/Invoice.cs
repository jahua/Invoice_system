using System;
using InvoiceSystem.Domain.Enums;

namespace InvoiceSystem.Domain.Entities
{
    public class Invoice
    {
        public int Id { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public int EmployeeId { get; set; }
        public int ContractId { get; set; }
        public Employee Employee { get; set; } = null!;
        public Contract Contract { get; set; } = null!;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int DaysWorked { get; set; }
        public decimal TotalAmount { get; set; }
        public InvoiceStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }
} 