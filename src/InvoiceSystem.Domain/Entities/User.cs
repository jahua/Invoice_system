using System;

namespace InvoiceSystem.Domain.Entities
{
    public class User
    {
        public int Id { get; set; }
        public required string Email { get; set; }
        public required string Username { get; set; }
        public required string PasswordHash { get; set; }
        public required string Role { get; set; } // "InvoiceManager" or "Employee"
        public int? EmployeeId { get; set; }
        public virtual Employee? Employee { get; set; }

        public bool IsManager => Role == "InvoiceManager";
    }
} 