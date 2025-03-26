using System;
using InvoiceSystem.Domain.Enums;
using InvoiceSystem.Domain.Entities;

namespace InvoiceSystem.Domain.DTOs
{
    public class ContractDto
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal DailyRate { get; set; }
        public PayGrade PayGrade { get; set; }
        public ContractType ContractType { get; set; }
        public virtual Employee? Employee { get; set; }
        public string ContractDisplayName => $"{ContractType} - {PayGrade} (${DailyRate}/day)";
    }

    public class CreateContractDto
    {
        public int EmployeeId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal DailyRate { get; set; }
        public PayGrade PayGrade { get; set; }
        public ContractType ContractType { get; set; }
    }

    public class UpdateContractDto
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal DailyRate { get; set; }
        public PayGrade PayGrade { get; set; }
        public ContractType ContractType { get; set; }
    }
} 
