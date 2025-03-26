using Microsoft.EntityFrameworkCore;
using InvoiceSystem.Domain.Entities;

namespace InvoiceSystem.Infrastructure.Data
{
    public class InvoiceSystemDbContext : DbContext
    {
        public InvoiceSystemDbContext(DbContextOptions<InvoiceSystemDbContext> options)
            : base(options)
        {
        }

        public required DbSet<Employee> Employees { get; set; }
        public required DbSet<Invoice> Invoices { get; set; }
        public required DbSet<Contract> Contracts { get; set; }
        public required DbSet<User> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Employee>()
                .HasIndex(e => e.Email)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();

            modelBuilder.Entity<Invoice>(entity =>
            {
                entity.HasKey(e => e.Id);
                
                entity.Property(e => e.InvoiceNumber)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.StartDate)
                    .IsRequired();

                entity.Property(e => e.EndDate)
                    .IsRequired();

                entity.Property(e => e.DaysWorked)
                    .IsRequired();

                entity.Property(e => e.TotalAmount)
                    .IsRequired();

                entity.Property(e => e.Status)
                    .IsRequired();

                entity.Property(e => e.CreatedAt)
                    .IsRequired();

                entity.HasOne(e => e.Employee)
                    .WithMany(e => e.Invoices)
                    .HasForeignKey(e => e.EmployeeId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Contract)
                    .WithMany()
                    .HasForeignKey(e => e.ContractId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
} 