using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using InvoiceSystem.Domain.Entities;
using InvoiceSystem.Domain.Interfaces;
using InvoiceSystem.Domain.Enums;
using InvoiceSystem.Infrastructure.Data;

namespace InvoiceSystem.Infrastructure.Services
{
    public class DataSeedingService : IDataSeedingService
    {
        private readonly InvoiceSystemDbContext _context;
        private readonly ILogger<DataSeedingService> _logger;
        private readonly IAuthService _authService;

        public DataSeedingService(
            InvoiceSystemDbContext context,
            ILogger<DataSeedingService> logger,
            IAuthService authService)
        {
            _context = context;
            _logger = logger;
            _authService = authService;
        }

        public async Task SeedAllDataAsync()
        {
            try
            {
                _logger.LogInformation("Starting data seeding process...");

                // Clear existing data first
                await ClearAllDataAsync();

                // 1. Seed Employees
                var employees = await SeedEmployeesAsync();
                _logger.LogInformation("Seeded {Count} employees", employees.Count);

                // 2. Seed Contracts
                var contracts = await SeedContractsAsync(employees);
                _logger.LogInformation("Seeded {Count} contracts", contracts.Count);

                // 3. Seed Users
                await SeedUsersAsync(employees);
                _logger.LogInformation("Seeded users");

                // 4. Seed Invoices
                await SeedInvoicesAsync(employees, contracts);
                _logger.LogInformation("Seeded invoices");

                _logger.LogInformation("Data seeding completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during data seeding");
                throw;
            }
        }

        public async Task ClearAllDataAsync()
        {
            try
            {
                _logger.LogInformation("Clearing existing data...");
                
                // Clear in correct order to respect foreign key constraints
                _context.Invoices.RemoveRange(_context.Invoices);
                _context.Contracts.RemoveRange(_context.Contracts);
                _context.Users.RemoveRange(_context.Users);
                _context.Employees.RemoveRange(_context.Employees);
                
                await _context.SaveChangesAsync();
                _logger.LogInformation("Existing data cleared successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing existing data");
                throw;
            }
        }

        private async Task<List<Employee>> SeedEmployeesAsync()
        {
            var employees = new List<Employee>
            {
                new Employee
                {
                    FirstName = "Robert",
                    LastName = "Wilson",
                    Email = "robert.wilson@example.com",
                    PhoneNumber = "111-222-3333",
                    Department = "Management",
                    Position = "Project Manager",
                    Salary = 85000,
                    HireDate = DateTime.UtcNow.AddYears(-6)
                },
                new Employee
                {
                    FirstName = "John",
                    LastName = "Doe",
                    Email = "john.doe@example.com",
                    PhoneNumber = "123-456-7890",
                    Department = "IT",
                    Position = "Senior Developer",
                    Salary = 75000,
                    HireDate = DateTime.UtcNow.AddYears(-5)
                },
                new Employee
                {
                    FirstName = "Jane",
                    LastName = "Smith",
                    Email = "jane.smith@example.com",
                    PhoneNumber = "098-765-4321",
                    Department = "Marketing",
                    Position = "Marketing Specialist",
                    Salary = 65000,
                    HireDate = DateTime.UtcNow.AddYears(-3)
                },
                new Employee
                {
                    FirstName = "Mike",
                    LastName = "Johnson",
                    Email = "mike.johnson@example.com",
                    PhoneNumber = "555-555-5555",
                    Department = "Sales",
                    Position = "Sales Representative",
                    Salary = 60000,
                    HireDate = DateTime.UtcNow.AddYears(-1)
                }
            };

            await _context.Employees.AddRangeAsync(employees);
            await _context.SaveChangesAsync();
            return employees;
        }

        private async Task<List<Contract>> SeedContractsAsync(List<Employee> employees)
        {
            var contracts = new List<Contract>();
            var now = DateTime.UtcNow;

            // Contract configurations
            var contractConfigs = new[]
            {
                (Employee: employees[0], DailyRate: 600M, PayGrade: PayGrade.Expert, ContractType: ContractType.FullTime),
                (Employee: employees[1], DailyRate: 500M, PayGrade: PayGrade.Expert, ContractType: ContractType.FullTime),
                (Employee: employees[2], DailyRate: 400M, PayGrade: PayGrade.Senior, ContractType: ContractType.FullTime),
                (Employee: employees[3], DailyRate: 300M, PayGrade: PayGrade.Intermediate, ContractType: ContractType.Contract)
            };

            foreach (var config in contractConfigs)
            {
                var contract = new Contract
                {
                    EmployeeId = config.Employee.Id,
                    Employee = config.Employee,
                    StartDate = config.Employee.HireDate,
                    EndDate = config.Employee.HireDate.AddYears(2),
                    DailyRate = config.DailyRate,
                    PayGrade = config.PayGrade,
                    ContractType = config.ContractType
                };

                contracts.Add(contract);
            }

            await _context.Contracts.AddRangeAsync(contracts);
            await _context.SaveChangesAsync();
            return contracts;
        }

        private async Task SeedUsersAsync(List<Employee> employees)
        {
            var users = new List<User>();

            // Find specific employees
            var manager = employees.First(e => e.FirstName == "Robert" && e.LastName == "Wilson");
            var johnDoe = employees.First(e => e.FirstName == "John" && e.LastName == "Doe");
            var janeSmith = employees.First(e => e.FirstName == "Jane" && e.LastName == "Smith");
            var mikeJohnson = employees.First(e => e.FirstName == "Mike" && e.LastName == "Johnson");

            // Create manager account
            users.Add(new User
            {
                Email = manager.Email,
                Username = "manager1",
                PasswordHash = _authService.HashPassword("password123"),
                Role = "InvoiceManager",
                EmployeeId = manager.Id
            });

            // Create employee accounts
            var employeeUsers = new[]
            {
                (Employee: johnDoe, Username: "employee1"),
                (Employee: janeSmith, Username: "employee2"),
                (Employee: mikeJohnson, Username: "employee3")
            };

            foreach (var (Employee, Username) in employeeUsers)
            {
                users.Add(new User
                {
                    Email = Employee.Email,
                    Username = Username,
                    PasswordHash = _authService.HashPassword("password123"),
                    Role = "Employee",
                    EmployeeId = Employee.Id
                });
            }

            await _context.Users.AddRangeAsync(users);
            await _context.SaveChangesAsync();
        }

        private async Task SeedInvoicesAsync(List<Employee> employees, List<Contract> contracts)
        {
            var invoices = new List<Invoice>();
            var now = DateTime.UtcNow;

            // Create some sample invoices within contract periods
            foreach (var employee in employees)
            {
                var contract = contracts.First(c => c.EmployeeId == employee.Id);
                var startDate = contract.StartDate.AddMonths(1); // Start one month after contract start
                var endDate = startDate.AddDays(15); // 15 days of work

                var invoice = new Invoice
                {
                    EmployeeId = employee.Id,
                    ContractId = contract.Id,
                    StartDate = startDate,
                    EndDate = endDate,
                    DaysWorked = 15,
                    TotalAmount = contract.DailyRate * 15,
                    Status = InvoiceStatus.Draft,
                    CreatedAt = endDate.AddDays(1)
                };

                invoices.Add(invoice);

                // Add another invoice for a different period
                startDate = endDate.AddMonths(1);
                endDate = startDate.AddDays(20);

                invoice = new Invoice
                {
                    EmployeeId = employee.Id,
                    ContractId = contract.Id,
                    StartDate = startDate,
                    EndDate = endDate,
                    DaysWorked = 20,
                    TotalAmount = contract.DailyRate * 20,
                    Status = InvoiceStatus.Approved,
                    CreatedAt = endDate.AddDays(1)
                };

                invoices.Add(invoice);
            }

            await _context.Invoices.AddRangeAsync(invoices);
            await _context.SaveChangesAsync();
        }
    }
} 