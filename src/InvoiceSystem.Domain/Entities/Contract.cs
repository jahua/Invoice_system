using System;
using InvoiceSystem.Domain.Enums;

namespace InvoiceSystem.Domain.Entities
{
    public class Contract
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal DailyRate { get; set; }
        public PayGrade PayGrade { get; set; }
        public ContractType ContractType { get; set; }
        
        public virtual required Employee Employee { get; set; }
    }
} 