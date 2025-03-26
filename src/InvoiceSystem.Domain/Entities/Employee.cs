using System;
using System.Collections.Generic;

namespace InvoiceSystem.Domain.Entities
{
    public class Employee
    {
        public Employee()
        {
            Contracts = new List<Contract>();
            Invoices = new List<Invoice>();
        }

        public int Id { get; set; }
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
        public required string Email { get; set; }
        public required string PhoneNumber { get; set; }
        public required string Department { get; set; }
        public required string Position { get; set; }
        public decimal Salary { get; set; }
        public DateTime HireDate { get; set; }
        public string FullName => $"{FirstName} {LastName}";
        
        public ICollection<Contract> Contracts { get; set; }
        public ICollection<Invoice> Invoices { get; set; }
    }
} 