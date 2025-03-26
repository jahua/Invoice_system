# Invoice System Prototype

This prototype demonstrates the key architectural components and implementation of the invoicing system as specified in the requirements.

## 1. Database Design

### Entity Relationship Diagram

```
+------------------+       +-------------------+       +-------------------+
|    Employee      |       |     Contract      |       |     Invoice       |
+------------------+       +-------------------+       +-------------------+
| PK: Id           |       | PK: Id            |       | PK: Id            |
| FirstName        |       | FK: EmployeeId    |----+  | FK: EmployeeId    |----+
| LastName         |<------| StartDate         |    |  | StartDate         |    |
| EmployeeIdentifier|      | EndDate           |    |  | EndDate           |    |
+------------------+       | DailyRate         |    |  | DaysWorked        |    |
        |                  +-------------------+    |  | TotalAmount       |    |
        |                                           |  +-------------------+    |
        |                                           |                           |
        +-----------+-------------------------------|---------------------------+
                    |                               |
                    |                               |
               +----v----+                          |
               |   User   |                          |
               +----------+                          |
               | PK: Id   |                          |
               | Username |                          |
               | Password |                          |
               | FK: EmployeeId |--------------------+
               | IsInvoiceManager |
               +----------+
```

### SQL DDL Script

```sql
CREATE TABLE Employees (
    Id INT PRIMARY KEY IDENTITY(1,1),
    FirstName NVARCHAR(100) NOT NULL,
    LastName NVARCHAR(100) NOT NULL,
    EmployeeIdentifier NVARCHAR(50) NOT NULL UNIQUE
);

CREATE TABLE Contracts (
    Id INT PRIMARY KEY IDENTITY(1,1),
    EmployeeId INT NOT NULL,
    StartDate DATE NOT NULL,
    EndDate DATE NOT NULL,
    DailyRate DECIMAL(18,2) NOT NULL,
    CONSTRAINT FK_Contract_Employee FOREIGN KEY (EmployeeId) REFERENCES Employees(Id),
    CONSTRAINT CK_Contract_EndDate_After_StartDate CHECK (EndDate >= StartDate),
    CONSTRAINT CK_Contract_DailyRate_Positive CHECK (DailyRate > 0)
);

CREATE TABLE Invoices (
    Id INT PRIMARY KEY IDENTITY(1,1),
    EmployeeId INT NOT NULL,
    StartDate DATE NOT NULL,
    EndDate DATE NOT NULL,
    DaysWorked INT NOT NULL,
    TotalAmount DECIMAL(18,2) NOT NULL,
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Invoice_Employee FOREIGN KEY (EmployeeId) REFERENCES Employees(Id),
    CONSTRAINT CK_Invoice_EndDate_After_StartDate CHECK (EndDate >= StartDate),
    CONSTRAINT CK_Invoice_DaysWorked_Positive CHECK (DaysWorked > 0)
);

CREATE TABLE Users (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Username NVARCHAR(50) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(100) NOT NULL,
    EmployeeId INT NULL,
    IsInvoiceManager BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_User_Employee FOREIGN KEY (EmployeeId) REFERENCES Employees(Id)
);
```

## 2. Data Seeding Script

```csharp
// InvoiceSystem.Infrastructure/Data/DbInitializer.cs
public static class DbInitializer
{
    public static async Task SeedAsync(InvoiceSystemDbContext context)
    {
        // Clear existing data
        context.Invoices.RemoveRange(context.Invoices);
        context.Contracts.RemoveRange(context.Contracts);
        context.Users.RemoveRange(context.Users);
        context.Employees.RemoveRange(context.Employees);
        await context.SaveChangesAsync();

        // Seed Employees
        var employees = new List<Employee>
        {
            new Employee { FirstName = "John", LastName = "Doe", EmployeeIdentifier = "EMP001" },
            new Employee { FirstName = "Jane", LastName = "Smith", EmployeeIdentifier = "EMP002" },
            new Employee { FirstName = "Michael", LastName = "Johnson", EmployeeIdentifier = "EMP003" }
        };
        
        await context.Employees.AddRangeAsync(employees);
        await context.SaveChangesAsync();

        // Seed Contracts
        var now = DateTime.Now;
        var contracts = new List<Contract>
        {
            new Contract { 
                EmployeeId = 1, 
                StartDate = now.AddMonths(-6), 
                EndDate = now.AddMonths(6), 
                DailyRate = 100.00m 
            },
            new Contract { 
                EmployeeId = 2, 
                StartDate = now.AddMonths(-3), 
                EndDate = now.AddMonths(9), 
                DailyRate = 120.00m 
            },
            new Contract { 
                EmployeeId = 3, 
                StartDate = now.AddDays(-45), 
                EndDate = now.AddDays(45), 
                DailyRate = 150.00m 
            }
        };
        
        await context.Contracts.AddRangeAsync(contracts);
        await context.SaveChangesAsync();

        // Seed Users
        var users = new List<User>
        {
            new User { 
                Username = "john.doe", 
                PasswordHash = "password", // In production use proper password hashing
                EmployeeId = 1,
                IsInvoiceManager = false
            },
            new User { 
                Username = "jane.smith", 
                PasswordHash = "password", 
                EmployeeId = 2,
                IsInvoiceManager = true
            },
            new User { 
                Username = "michael.johnson", 
                PasswordHash = "password", 
                EmployeeId = 3,
                IsInvoiceManager = false
            }
        };
        
        await context.Users.AddRangeAsync(users);
        await context.SaveChangesAsync();

        // Seed Invoices
        var invoices = new List<Invoice>
        {
            new Invoice {
                EmployeeId = 1,
                StartDate = now.AddDays(-30),
                EndDate = now.AddDays(-15),
                DaysWorked = 16,
                TotalAmount = 1600.00m, // 16 days * $100
                CreatedAt = now.AddDays(-14)
            },
            new Invoice {
                EmployeeId = 2,
                StartDate = now.AddDays(-20),
                EndDate = now.AddDays(-10),
                DaysWorked = 11,
                TotalAmount = 1320.00m, // 11 days * $120
                CreatedAt = now.AddDays(-9)
            }
        };
        
        await context.Invoices.AddRangeAsync(invoices);
        await context.SaveChangesAsync();
    }
}
```

## 3. Domain Layer (Core Entities and Business Rules)

```csharp
// InvoiceSystem.Domain/Entities/Employee.cs
public class Employee
{
    public int Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string EmployeeIdentifier { get; set; }
    
    // Navigation properties
    public virtual ICollection<Contract> Contracts { get; set; }
    public virtual ICollection<Invoice> Invoices { get; set; }
    
    public string FullName => $"{FirstName} {LastName}";
    
    // Business logic methods
    public Contract GetActiveContract(DateTime date)
    {
        return Contracts?.FirstOrDefault(c => 
            c.StartDate <= date && c.EndDate >= date);
    }
    
    public Contract GetContractForPeriod(DateTime startDate, DateTime endDate)
    {
        return Contracts?.FirstOrDefault(c => 
            c.StartDate <= startDate && c.EndDate >= endDate);
    }
    
    public bool HasOverlappingInvoice(DateTime startDate, DateTime endDate, int? excludeInvoiceId = null)
    {
        return Invoices?.Any(i => 
            i.Id != excludeInvoiceId &&
            i.StartDate <= endDate && 
            i.EndDate >= startDate) ?? false;
    }
}

// InvoiceSystem.Domain/Entities/Contract.cs
public class Contract
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal DailyRate { get; set; }
    
    // Navigation property
    public virtual Employee Employee { get; set; }
    
    // Business logic methods
    public bool IsValidForPeriod(DateTime startDate, DateTime endDate)
    {
        return StartDate <= startDate && EndDate >= endDate;
    }
    
    public decimal CalculateAmount(int daysWorked)
    {
        return DailyRate * daysWorked;
    }
}

// InvoiceSystem.Domain/Entities/Invoice.cs
public class Invoice
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int DaysWorked { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; }
    
    // Navigation property
    public virtual Employee Employee { get; set; }
    
    // Business logic methods
    public int MaxPossibleDays()
    {
        return (EndDate - StartDate).Days + 1;
    }
    
    public bool IsValidDaysWorked()
    {
        return DaysWorked <= MaxPossibleDays();
    }
}

// InvoiceSystem.Domain/Entities/User.cs
public class User
{
    public int Id { get; set; }
    public string Username { get; set; }
    public string PasswordHash { get; set; }
    public int? EmployeeId { get; set; }
    public bool IsInvoiceManager { get; set; }
    
    // Navigation property
    public virtual Employee Employee { get; set; }
}

// InvoiceSystem.Domain/Exceptions/DomainException.cs
public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}

// InvoiceSystem.Domain/Services/InvoiceValidationService.cs
public interface IInvoiceValidationService
{
    ValidationResult ValidateInvoice(Invoice invoice, Employee employee);
}

public class InvoiceValidationService : IInvoiceValidationService
{
    public ValidationResult ValidateInvoice(Invoice invoice, Employee employee)
    {
        var result = new ValidationResult();
        
        // R7: Validate that employee has a valid contract for the invoice period
        var contract = employee.GetContractForPeriod(invoice.StartDate, invoice.EndDate);
        if (contract == null)
        {
            result.AddError("No valid contract exists for this invoice period.");
            return result;
        }
        
        // R8: Validate that days worked fits within the invoice period
        int maxPossibleDays = invoice.MaxPossibleDays();
        if (invoice.DaysWorked > maxPossibleDays)
        {
            result.AddError($"Days worked ({invoice.DaysWorked}) exceeds the maximum possible days in the period ({maxPossibleDays}).");
        }
        
        // R9: Validate the total amount against the daily rate
        decimal expectedAmount = contract.CalculateAmount(invoice.DaysWorked);
        if (invoice.TotalAmount != expectedAmount)
        {
            result.AddError($"Total amount ({invoice.TotalAmount}) does not match the expected amount ({expectedAmount}).");
        }
        
        // R10: Validate that there are no overlapping invoices
        if (employee.HasOverlappingInvoice(invoice.StartDate, invoice.EndDate, invoice.Id))
        {
            result.AddError("Employee already has an invoice for this period.");
        }
        
        return result;
    }
}

public class ValidationResult
{
    private List<string> _errors = new List<string>();
    
    public IReadOnlyCollection<string> Errors => _errors.AsReadOnly();
    
    public bool IsValid => !_errors.Any();
    
    public void AddError(string error)
    {
        _errors.Add(error);
    }
}
```

## 4. Application Layer (Services and DTOs)

```csharp
// InvoiceSystem.Application/DTOs/EmployeeDto.cs
public class EmployeeDto
{
    public int Id { get; set; }
    public string FullName { get; set; }
    public string EmployeeIdentifier { get; set; }
}

// InvoiceSystem.Application/DTOs/InvoiceDto.cs
public class InvoiceDto
{
    public int Id { get; set; }
    public string EmployeeName { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int DaysWorked { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; }
}

// InvoiceSystem.Application/DTOs/CreateInvoiceDto.cs
public class CreateInvoiceDto
{
    [Required(ErrorMessage = "Employee is required")]
    [Display(Name = "Employee")]
    public int EmployeeId { get; set; }
    
    [Required(ErrorMessage = "Start date is required")]
    [Display(Name = "Start Date")]
    [DataType(DataType.Date)]
    public DateTime StartDate { get; set; }
    
    [Required(ErrorMessage = "End date is required")]
    [Display(Name = "End Date")]
    [DataType(DataType.Date)]
    public DateTime EndDate { get; set; }
    
    [Required(ErrorMessage = "Days worked is required")]
    [Range(1, 366, ErrorMessage = "Days worked must be between 1 and 366")]
    [Display(Name = "Days Worked")]
    public int DaysWorked { get; set; }
}

// InvoiceSystem.Application/Services/InvoiceService.cs
public interface IInvoiceService
{
    Task<List<InvoiceDto>> GetAllInvoicesAsync();
    Task<List<InvoiceDto>> GetEmployeeInvoicesAsync(int employeeId);
    Task<InvoiceDto> GetInvoiceByIdAsync(int id);
    Task<ApplicationResult> CreateInvoiceAsync(CreateInvoiceDto dto);
}

public class InvoiceService : IInvoiceService
{
    private readonly IInvoiceSystemDbContext _dbContext;
    private readonly IInvoiceValidationService _validationService;
    
    public InvoiceService(
        IInvoiceSystemDbContext dbContext,
        IInvoiceValidationService validationService)
    {
        _dbContext = dbContext;
        _validationService = validationService;
    }
    
    public async Task<List<InvoiceDto>> GetAllInvoicesAsync()
    {
        return await _dbContext.Invoices
            .Include(i => i.Employee)
            .Select(i => new InvoiceDto
            {
                Id = i.Id,
                EmployeeName = i.Employee.FullName,
                StartDate = i.StartDate,
                EndDate = i.EndDate,
                DaysWorked = i.DaysWorked,
                TotalAmount = i.TotalAmount,
                CreatedAt = i.CreatedAt
            })
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync();
    }
    
    public async Task<List<InvoiceDto>> GetEmployeeInvoicesAsync(int employeeId)
    {
        return await _dbContext.Invoices
            .Include(i => i.Employee)
            .Where(i => i.EmployeeId == employeeId)
            .Select(i => new InvoiceDto
            {
                Id = i.Id,
                EmployeeName = i.Employee.FullName,
                StartDate = i.StartDate,
                EndDate = i.EndDate,
                DaysWorked = i.DaysWorked,
                TotalAmount = i.TotalAmount,
                CreatedAt = i.CreatedAt
            })
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync();
    }
    
    public async Task<InvoiceDto> GetInvoiceByIdAsync(int id)
    {
        var invoice = await _dbContext.Invoices
            .Include(i => i.Employee)
            .FirstOrDefaultAsync(i => i.Id == id);
            
        if (invoice == null)
            return null;
            
        return new InvoiceDto
        {
            Id = invoice.Id,
            EmployeeName = invoice.Employee.FullName,
            StartDate = invoice.StartDate,
            EndDate = invoice.EndDate,
            DaysWorked = invoice.DaysWorked,
            TotalAmount = invoice.TotalAmount,
            CreatedAt = invoice.CreatedAt
        };
    }
    
    public async Task<ApplicationResult> CreateInvoiceAsync(CreateInvoiceDto dto)
    {
        var result = new ApplicationResult();
        
        // Get employee with contracts and invoices
        var employee = await _dbContext.Employees
            .Include(e => e.Contracts)
            .Include(e => e.Invoices)
            .FirstOrDefaultAsync(e => e.Id == dto.EmployeeId);
            
        if (employee == null)
        {
            result.AddError("Employee not found");
            return result;
        }
        
        // Get the contract for this period
        var contract = employee.GetContractForPeriod(dto.StartDate, dto.EndDate);
        if (contract == null)
        {
            result.AddError("No valid contract for this invoice period");
            return result;
        }
        
        // Calculate the expected amount
        decimal expectedAmount = contract.CalculateAmount(dto.DaysWorked);
        
        // Create the invoice
        var invoice = new Invoice
        {
            EmployeeId = dto.EmployeeId,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            DaysWorked = dto.DaysWorked,
            TotalAmount = expectedAmount,
            CreatedAt = DateTime.Now
        };
        
        // Validate the invoice
        var validationResult = _validationService.ValidateInvoice(invoice, employee);
        if (!validationResult.IsValid)
        {
            foreach (var error in validationResult.Errors)
            {
                result.AddError(error);
            }
            return result;
        }
        
        // Save the invoice
        await _dbContext.Invoices.AddAsync(invoice);
        await _dbContext.SaveChangesAsync();
        
        result.Data = invoice.Id;
        return result;
    }
}

public class ApplicationResult
{
    private List<string> _errors = new List<string>();
    
    public IReadOnlyCollection<string> Errors => _errors.AsReadOnly();
    
    public bool IsSuccess => !_errors.Any();
    
    public object Data { get; set; }
    
    public void AddError(string error)
    {
        _errors.Add(error);
    }
}

// InvoiceSystem.Application/Services/EmployeeService.cs
public interface IEmployeeService
{
    Task<List<EmployeeDto>> GetAllEmployeesAsync();
}

public class EmployeeService : IEmployeeService
{
    private readonly IInvoiceSystemDbContext _dbContext;
    
    public EmployeeService(IInvoiceSystemDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    
    public async Task<List<EmployeeDto>> GetAllEmployeesAsync()
    {
        return await _dbContext.Employees
            .Select(e => new EmployeeDto
            {
                Id = e.Id,
                FullName = e.FullName,
                EmployeeIdentifier = e.EmployeeIdentifier
            })
            .ToListAsync();
    }
}
```

## 5. Infrastructure Layer (Data Access)

```csharp
// InvoiceSystem.Infrastructure/Data/InvoiceSystemDbContext.cs
public interface IInvoiceSystemDbContext
{
    DbSet<Employee> Employees { get; }
    DbSet<Contract> Contracts { get; }
    DbSet<Invoice> Invoices { get; }
    DbSet<User> Users { get; }
    
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

public class InvoiceSystemDbContext : DbContext, IInvoiceSystemDbContext
{
    public InvoiceSystemDbContext(DbContextOptions<InvoiceSystemDbContext> options)
        : base(options)
    {
    }
    
    public DbSet<Employee> Employees { get; set; }
    public DbSet<Contract> Contracts { get; set; }
    public DbSet<Invoice> Invoices { get; set; }
    public DbSet<User> Users { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Employee configuration
        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.EmployeeIdentifier).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => e.EmployeeIdentifier).IsUnique();
        });
        
        // Contract configuration
        modelBuilder.Entity<Contract>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.StartDate).IsRequired();
            entity.Property(c => c.EndDate).IsRequired();
            entity.Property(c => c.DailyRate).IsRequired().HasColumnType("decimal(18,2)");
            
            entity.HasOne(c => c.Employee)
                .WithMany(e => e.Contracts)
                .HasForeignKey(c => c.EmployeeId);
                
            entity.HasCheckConstraint("CK_Contract_EndDate_After_StartDate", "EndDate >= StartDate");
            entity.HasCheckConstraint("CK_Contract_DailyRate_Positive", "DailyRate > 0");
        });
        
        // Invoice configuration
        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.HasKey(i => i.Id);
            entity.Property(i => i.StartDate).IsRequired();
            entity.Property(i => i.EndDate).IsRequired();
            entity.Property(i => i.DaysWorked).IsRequired();
            entity.Property(i => i.TotalAmount).IsRequired().HasColumnType("decimal(18,2)");
            entity.Property(i => i.CreatedAt).IsRequired();
            
            entity.HasOne(i => i.Employee)
                .WithMany(e => e.Invoices)
                .HasForeignKey(i => i.EmployeeId);
                
            entity.HasCheckConstraint("CK_Invoice_EndDate_After_StartDate", "EndDate >= StartDate");
            entity.HasCheckConstraint("CK_Invoice_DaysWorked_Positive", "DaysWorked > 0");
        });
        
        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.Property(u => u.Username).IsRequired().HasMaxLength(50);
            entity.Property(u => u.PasswordHash).IsRequired().HasMaxLength(100);
            entity.Property(u => u.IsInvoiceManager).IsRequired();
            
            entity.HasIndex(u => u.Username).IsUnique();
            
            entity.HasOne(u => u.Employee)
                .WithMany()
                .HasForeignKey(u => u.EmployeeId)
                .IsRequired(false);
        });
    }
}

// InvoiceSystem.Infrastructure/Identity/AuthService.cs
public interface IAuthService
{
    Task<User> AuthenticateAsync(string username, string password);
    Task<bool> IsInvoiceManagerAsync(int userId);
    Task<int?> GetEmployeeIdAsync(int userId);
}

public class AuthService : IAuthService
{
    private readonly IInvoiceSystemDbContext _dbContext;
    
    public AuthService(IInvoiceSystemDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    
    public async Task<User> AuthenticateAsync(string username, string password)
    {
        // In production, use proper password hashing
        var user = await _dbContext.Users
            .Include(u => u.Employee)
            .FirstOrDefaultAsync(u => u.Username == username && u.PasswordHash == password);
            
        return user;
    }
    
    public async Task<bool> IsInvoiceManagerAsync(int userId)
    {
        var user = await _dbContext.Users.FindAsync(userId);
        return user?.IsInvoiceManager ?? false;
    }
    
    public async Task<int?> GetEmployeeIdAsync(int userId)
    {
        var user = await _dbContext.Users.FindAsync(userId);
        return user?.EmployeeId;
    }
}
```

## 6. Presentation Layer (MVC Controllers and Views)

```csharp
// InvoiceSystem.Web/Controllers/AccountController.cs
public class AccountController : Controller
{
    private readonly IAuthService _authService;
    
    public AccountController(IAuthService authService)
    {
        _authService = authService;
    }
    
    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }
    
    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);
            
        var user = await _authService.AuthenticateAsync(model.Username, model.Password);
        
        if (user == null)
        {
            ModelState.AddModelError("", "Invalid username or password.");
            return View(model);
        }
        
        // Set up claims identity
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username)
        };
        
        if (user.IsInvoiceManager)
            claims.Add(new Claim(ClaimTypes.Role, "InvoiceManager"));
            
        if (user.EmployeeId.HasValue)
        {
            claims.Add(new Claim(ClaimTypes.Role, "Employee"));
            claims.Add(new Claim("EmployeeId", user.EmployeeId.Value.ToString()));
        }
        
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme, 
            principal);
            
        return RedirectToAction("Index", "Invoice");
    }
    
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login");
    }
}

// InvoiceSystem.Web/Controllers/InvoiceController.cs
[Authorize]
public class InvoiceController : Controller
{
    private readonly IInvoiceService _invoiceService;
    private readonly IEmployeeService _employeeService;
    private readonly IAuthService _authService;
    
    public InvoiceController(
        IInvoiceService invoiceService,
        IEmployeeService employeeService,
        IAuthService authService)
    {
        _invoiceService = invoiceService;
        _employeeService = employeeService;
        _authService = authService;
    }
    
    public async Task<IActionResult> Index()
    {
        try
        {
            List<InvoiceDto> invoices;
            
            if (User.IsInRole("InvoiceManager"))
            {
                // Invoice managers can see all invoices
                invoices = await _invoiceService.GetAllInvoicesAsync();
            }
            else if (User.IsInRole("Employee"))
            {
                // Employees can only see their own invoices
                var employeeIdClaim = User.FindFirst("EmployeeId");
                if (employeeIdClaim == null)
                    return Forbid();
                    
                int employeeId = int.Parse(employeeIdClaim.Value);
                invoices = await _invoiceService.GetEmployeeInvoicesAsync(employeeId);
            }
            else
            {
                return Forbid();
            }
            
            return View(invoices);
        }
        catch (Exception ex)
        {
            // In production, log the exception
            TempData["ErrorMessage"] = "An error occurred: " + ex.Message;
            return View(new List<InvoiceDto>());
        }
    }
    
    [HttpGet]
    [Authorize(Roles = "InvoiceManager")]
    public async Task<IActionResult> Create()
    {
        try
        {
            var employees = await _employeeService.GetAllEmployeesAsync();
            ViewBag.Employees = new SelectList(employees, "Id", "FullName");
            
            var model = new CreateInvoiceViewModel
            {
                StartDate = DateTime.Today,
                EndDate = DateTime.Today.AddDays(30)
            };
            
            return View(model);
        }
        catch (Exception ex)
        {
            // In production, log the exception
            TempData["ErrorMessage"] = "An error occurred: " + ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }
    
    [HttpPost]
    [Authorize(Roles = "InvoiceManager")]
    public async Task<IActionResult> Create(CreateInvoiceViewModel model)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var employees = await _employeeService.GetAllEmployeesAsync();
                ViewBag.Employees = new SelectList(employees, "Id", "FullName");
                return View(model);
            }
            
            TempData["SuccessMessage"] = "Invoice created successfully.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            // In production, log the exception
            ModelState.AddModelError("", "An error occurred: " + ex.Message);
            
            var employees = await _employeeService.GetAllEmployeesAsync();
            ViewBag.Employees = new SelectList(employees, "Id", "FullName");
            
            return View(model);
        }
    }
    
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        try
        {
            var invoice = await _invoiceService.GetInvoiceByIdAsync(id);
            
            if (invoice == null)
                return NotFound();
                
            // Check if user has permission to view this invoice
            if (!User.IsInRole("InvoiceManager"))
            {
                var employeeIdClaim = User.FindFirst("EmployeeId");
                if (employeeIdClaim == null)
                    return Forbid();
                    
                int employeeId = int.Parse(employeeIdClaim.Value);
                
                // Get employee name from the invoice
                var employeeNameParts = invoice.EmployeeName.Split(' ');
                var invoiceEmployeeId = await _authService.GetEmployeeIdAsync(int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)));
                
                if (invoiceEmployeeId != employeeId)
                    return Forbid();
            }
            
            return View(invoice);
        }
        catch (Exception ex)
        {
            // In production, log the exception
            TempData["ErrorMessage"] = "An error occurred: " + ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }
}

// InvoiceSystem.Web/ViewModels/LoginViewModel.cs
public class LoginViewModel
{
    [Required]
    [Display(Name = "Username")]
    public string Username { get; set; }
    
    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; }
}

// InvoiceSystem.Web/ViewModels/CreateInvoiceViewModel.cs
public class CreateInvoiceViewModel
{
    [Required]
    [Display(Name = "Employee")]
    public int EmployeeId { get; set; }
    
    [Required]
    [Display(Name = "Start Date")]
    [DataType(DataType.Date)]
    public DateTime StartDate { get; set; }
    
    [Required]
    [Display(Name = "End Date")]
    [DataType(DataType.Date)]
    public DateTime EndDate { get; set; }
    
    [Required]
    [Display(Name = "Days Worked")]
    [Range(1, 366, ErrorMessage = "Days worked must be between 1 and 366")]
    public int DaysWorked { get; set; }
}
```

## 7. Dependency Injection Configuration

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddDbContext<InvoiceSystemDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register interfaces and implementations
builder.Services.AddScoped<IInvoiceSystemDbContext>(provider => 
    provider.GetRequiredService<InvoiceSystemDbContext>());
builder.Services.AddScoped<IInvoiceValidationService, InvoiceValidationService>();
builder.Services.AddScoped<IInvoiceService, InvoiceService>();
builder.Services.AddScoped<IEmployeeService, EmployeeService>();
builder.Services.AddScoped<IAuthService, AuthService>();

// Add authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
    });

// Add authorization
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("InvoiceManager", policy =>
        policy.RequireRole("InvoiceManager"));
    options.AddPolicy("Employee", policy =>
        policy.RequireRole("Employee"));
});

builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    
    // Initialize database with seed data in development
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        var context = services.GetRequiredService<InvoiceSystemDbContext>();
        DbInitializer.SeedAsync(context).Wait();
    }
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Invoice}/{action=Index}/{id?}");

app.Run();
```

## 8. Views (Razor)

```html
<!-- Views/Account/Login.cshtml -->
@model LoginViewModel

@{
    ViewData["Title"] = "Login";
}

<div class="row justify-content-center">
    <div class="col-md-6">
        <div class="card">
            <div class="card-header">
                <h2 class="text-center">Company XYZ Invoice System</h2>
            </div>
            <div class="card-body">
                <h4 class="card-title">Login</h4>
                <form asp-action="Login" method="post">
                    <div asp-validation-summary="ModelOnly" class="text-danger"></div>
                    
                    <div class="form-group mb-3">
                        <label asp-for="Username"></label>
                        <input asp-for="Username" class="form-control" />
                        <span asp-validation-for="Username" class="text-danger"></span>
                    </div>
                    
                    <div class="form-group mb-3">
                        <label asp-for="Password"></label>
                        <input asp-for="Password" class="form-control" />
                        <span asp-validation-for="Password" class="text-danger"></span>
                    </div>
                    
                    <div class="form-group mt-4">
                        <button type="submit" class="btn btn-primary w-100">Login</button>
                    </div>
                </form>
            </div>
            <div class="card-footer text-muted">
                <p class="small text-center">Test credentials: john.doe/password (employee) or jane.smith/password (invoice manager)</p>
            </div>
        </div>
    </div>
</div>

<!-- Views/Invoice/Index.cshtml -->
@model List<InvoiceDto>

@{
    ViewData["Title"] = "Invoices";
}

<div class="d-flex justify-content-between align-items-center mb-4">
    <h1>Invoices</h1>
    
    @if (User.IsInRole("InvoiceManager"))
    {
        <a asp-action="Create" class="btn btn-primary">
            <i class="bi bi-plus"></i> Create New Invoice
        </a>
    }
</div>

@if (TempData["SuccessMessage"] != null)
{
    <div class="alert alert-success alert-dismissible fade show" role="alert">
        @TempData["SuccessMessage"]
        <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
    </div>
}

@if (TempData["ErrorMessage"] != null)
{
    <div class="alert alert-danger alert-dismissible fade show" role="alert">
        @TempData["ErrorMessage"]
        <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
    </div>
}

@if (!Model.Any())
{
    <div class="alert alert-info">
        No invoices found.
    </div>
}
else
{
    <div class="table-responsive">
        <table class="table table-striped table-hover">
            <thead class="table-light">
                <tr>
                    <th>ID</th>
                    <th>Employee</th>
                    <th>Period</th>
                    <th>Days Worked</th>
                    <th>Amount</th>
                    <th>Created</th>
                    <th></th>
                </tr>
            </thead>
            <tbody>
                @foreach (var invoice in Model)
                {
                    <tr>
                        <td>@invoice.Id</td>
                        <td>@invoice.EmployeeName</td>
                        <td>@invoice.StartDate.ToShortDateString() - @invoice.EndDate.ToShortDateString()</td>
                        <td>@invoice.DaysWorked</td>
                        <td>$@invoice.TotalAmount.ToString("N2")</td>
                        <td>@invoice.CreatedAt.ToShortDateString()</td>
                        <td>
                            <a asp-action="Details" asp-route-id="@invoice.Id" class="btn btn-outline-primary btn-sm">
                                <i class="bi bi-eye"></i> Details
                            </a>
                        </td>
                    </tr>
                }
            </tbody>
        </table>
    </div>
}

<!-- Views/Invoice/Create.cshtml -->
@model CreateInvoiceViewModel

@{
    ViewData["Title"] = "Create Invoice";
}

<div class="row">
    <div class="col-md-8">
        <div class="card">
            <div class="card-header">
                <h2>Create New Invoice</h2>
            </div>
            <div class="card-body">
                <form asp-action="Create" method="post">
                    <div asp-validation-summary="ModelOnly" class="text-danger"></div>
                    
                    <div class="form-group mb-3">
                        <label asp-for="EmployeeId" class="form-label"></label>
                        <select asp-for="EmployeeId" class="form-select" asp-items="ViewBag.Employees">
                            <option value="">-- Select Employee --</option>
                        </select>
                        <span asp-validation-for="EmployeeId" class="text-danger"></span>
                    </div>
                    
                    <div class="row mb-3">
                        <div class="col-md-6">
                            <label asp-for="StartDate" class="form-label"></label>
                            <input asp-for="StartDate" class="form-control" />
                            <span asp-validation-for="StartDate" class="text-danger"></span>
                        </div>
                        <div class="col-md-6">
                            <label asp-for="EndDate" class="form-label"></label>
                            <input asp-for="EndDate" class="form-control" />
                            <span asp-validation-for="EndDate" class="text-danger"></span>
                        </div>
                    </div>
                    
                    <div class="form-group mb-3">
                        <label asp-for="DaysWorked" class="form-label"></label>
                        <input asp-for="DaysWorked" class="form-control" />
                        <span asp-validation-for="DaysWorked" class="text-danger"></span>
                    </div>
                    
                    <div class="form-group mt-4">
                        <button type="submit" class="btn btn-primary">Create</button>
                        <a asp-action="Index" class="btn btn-outline-secondary">Back to List</a>
                    </div>
                </form>
            </div>
        </div>
    </div>
    <div class="col-md-4">
        <div class="card">
            <div class="card-header bg-info text-white">
                <h5 class="mb-0">Invoice Validation Rules</h5>
            </div>
            <div class="card-body">
                <ul class="list-group list-group-flush">
                    <li class="list-group-item">
                        <i class="bi bi-check-circle text-success"></i>
                        Employee must have a valid contract for the entire invoice period
                    </li>
                    <li class="list-group-item">
                        <i class="bi bi-check-circle text-success"></i>
                        Days worked must fit within the invoice period
                    </li>
                    <li class="list-group-item">
                        <i class="bi bi-check-circle text-success"></i>
                        Total amount is calculated based on the contract's daily rate
                    </li>
                    <li class="list-group-item">
                        <i class="bi bi-check-circle text-success"></i>
                        Employee cannot have overlapping invoices
                    </li>
                </ul>
            </div>
        </div>
    </div>
</div>

<!-- Views/Invoice/Details.cshtml -->
@model InvoiceDto

@{
    ViewData["Title"] = "Invoice Details";
}

<div class="card">
    <div class="card-header bg-primary text-white">
        <h2>Invoice #@Model.Id</h2>
    </div>
    <div class="card-body">
        <div class="row mb-3">
            <div class="col-md-2 fw-bold">Employee:</div>
            <div class="col-md-10">@Model.EmployeeName</div>
        </div>
        <div class="row mb-3">
            <div class="col-md-2 fw-bold">Period:</div>
            <div class="col-md-10">@Model.StartDate.ToShortDateString() - @Model.EndDate.ToShortDateString()</div>
        </div>
        <div class="row mb-3">
            <div class="col-md-2 fw-bold">Days Worked:</div>
            <div class="col-md-10">@Model.DaysWorked</div>
        </div>
        <div class="row mb-3">
            <div class="col-md-2 fw-bold">Amount:</div>
            <div class="col-md-10">$@Model.TotalAmount.ToString("N2")</div>
        </div>
        <div class="row mb-3">
            <div class="col-md-2 fw-bold">Created:</div>
            <div class="col-md-10">@Model.CreatedAt</div>
        </div>
    </div>
    <div class="card-footer">
        <a asp-action="Index" class="btn btn-outline-primary">Back to List</a>
    </div>
</div>

<!-- Views/Shared/_Layout.cshtml -->
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>@ViewData["Title"] - Company XYZ Invoice System</title>
    <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap@5.2.3/dist/css/bootstrap.min.css" />
    <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.10.3/font/bootstrap-icons.css">
    <link rel="stylesheet" href="~/css/site.css" asp-append-version="true" />
</head>
<body>
    <header>
        <nav class="navbar navbar-expand-sm navbar-toggleable-sm navbar-dark bg-dark border-bottom box-shadow mb-3">
            <div class="container">
                <a class="navbar-brand" asp-controller="Invoice" asp-action="Index">Company XYZ Invoice System</a>
                <button class="navbar-toggler" type="button" data-bs-toggle="collapse" data-bs-target="#navbarSupportedContent"
                        aria-controls="navbarSupportedContent" aria-expanded="false" aria-label="Toggle navigation">
                    <span class="navbar-toggler-icon"></span>
                </button>
                <div class="collapse navbar-collapse" id="navbarSupportedContent">
                    <ul class="navbar-nav me-auto mb-2 mb-lg-0">
                        <li class="nav-item">
                            <a class="nav-link" asp-controller="Invoice" asp-action="Index">Invoices</a>
                        </li>
                    </ul>
                    <partial name="_LoginPartial" />
                </div>
            </div>
        </nav>
    </header>
    
    <div class="container">
        <main role="main" class="pb-3">
            @RenderBody()
        </main>
    </div>

    <footer class="border-top footer text-muted">
        <div class="container">
            &copy; @DateTime.Now.Year - Company XYZ Invoice System
        </div>
    </footer>

    <script src="https://cdn.jsdelivr.net/npm/bootstrap@5.2.3/dist/js/bootstrap.bundle.min.js"></script>
    <script src="~/js/site.js" asp-append-version="true"></script>
    @await RenderSectionAsync("Scripts", required: false)
</body>
</html>

<!-- Views/Shared/_LoginPartial.cshtml -->
@if (User.Identity.IsAuthenticated)
{
    <ul class="navbar-nav">
        <li class="nav-item">
            <span class="nav-link text-light">Hello, @User.Identity.Name!</span>
        </li>
        <li class="nav-item">
            <form asp-controller="Account" asp-action="Logout" method="post" id="logoutForm">
                <button type="submit" class="btn btn-link nav-link">Logout</button>
            </form>
        </li>
    </ul>
}
else
{
    <ul class="navbar-nav">
        <li class="nav-item">
            <a class="nav-link" asp-controller="Account" asp-action="Login">Login</a>
        </li>
    </ul>
}
```

## Project Evaluation Against Requirements

1. **Database Design (30 points)**
   - Properly designed entities for Employee, Contract, Invoice, and User
   - Clear relationships between entities reflecting the business domain
   - Database constraints enforcing business rules (R4, R5, R7, R8, R9, R10)
   - Unique constraints on appropriate fields (employee identifier, username)

2. **Data Seeding (20 points)**
   - Comprehensive seeding of all required entities
   - Maintenance of referential integrity
   - Creation of test accounts with different roles
   - Creation of sample invoices and contracts

3. **Web Application (50 points)**
   - Clean architecture with separation of concerns
     - Domain layer for business entities and rules
     - Application layer for services and DTOs
     - Infrastructure layer for data access
     - Presentation layer with MVC pattern
   - Proper authentication and authorization
     - Role-based access control for invoice managers vs employees
     - Login functionality with claims-based identity
   - Invoice management functionality
     - List invoices with appropriate filtering based on user role
     - Create new invoices with validation
     - View invoice details
   - Comprehensive business rule validation
     - Contract validity period validation (R7)
     - Days worked validation (R8)
     - Total amount validation (R9)
     - Overlapping invoice validation (R10)

This prototype addresses all the key requirements while demonstrating good software architecture principles. The clean separation of concerns, rich domain model, and proper validation of business rules showcase a well-designed system that could be easily extended for additional functionality.employeeService.GetAllEmployeesAsync();
                ViewBag.Employees = new SelectList(employees, "Id", "FullName");
                return View(model);
            }
            
            var dto = new CreateInvoiceDto
            {
                EmployeeId = model.EmployeeId,
                StartDate = model.StartDate,
                EndDate = model.EndDate,
                DaysWorked = model.DaysWorked
            };
            
            var result = await _invoiceService.CreateInvoiceAsync(dto);
            
            if (!result.IsSuccess)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error);
                }
                
                var employees = await _