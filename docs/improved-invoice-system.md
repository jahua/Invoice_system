# Improved Invoice System - Implementation Blueprint

## Revised Project Structure

```
InvoiceSystem/
├── src/
│   ├── InvoiceSystem.Web/                  # Presentation Layer
│   ├── InvoiceSystem.Application/          # Application Layer
│   ├── InvoiceSystem.Domain/               # Domain Layer
│   └── InvoiceSystem.Infrastructure/       # Infrastructure Layer
├── tests/
│   ├── InvoiceSystem.UnitTests/            # Unit tests
│   └── InvoiceSystem.IntegrationTests/     # Integration tests
└── db/
    ├── Migrations/                         # Database migrations
    └── Scripts/                            # Seeding scripts
```

## Database Design (ER Diagram)

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

## DDL SQL Scripts

```sql
CREATE TABLE Employees (
    Id INT PRIMARY KEY IDENTITY(1,1),
    FirstName NVARCHAR(100) NOT NULL,
    LastName NVARCHAR(100) NOT NULL,
    EmployeeIdentifier NVARCHAR(50) NOT NULL,
    CONSTRAINT UQ_Employee_Identifier UNIQUE (EmployeeIdentifier)
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

-- Index for faster contract lookups by employee
CREATE INDEX IX_Contract_EmployeeId ON Contracts(EmployeeId);

-- This function will be used to check for overlapping contracts
CREATE FUNCTION dbo.CheckOverlappingContracts (
    @EmployeeId INT, 
    @StartDate DATE,
    @EndDate DATE,
    @ExcludeContractId INT = NULL
)
RETURNS BIT
AS
BEGIN
    DECLARE @HasOverlap BIT = 0;
    
    IF EXISTS (
        SELECT 1 FROM Contracts
        WHERE EmployeeId = @EmployeeId
        AND (@ExcludeContractId IS NULL OR Id != @ExcludeContractId)
        AND (
            (StartDate <= @EndDate AND EndDate >= @StartDate)
        )
    )
        SET @HasOverlap = 1;
        
    RETURN @HasOverlap;
END;

-- Trigger to prevent overlapping contracts for the same employee
CREATE TRIGGER TR_Prevent_Overlapping_Contracts
ON Contracts
AFTER INSERT, UPDATE
AS
BEGIN
    IF EXISTS (
        SELECT 1 FROM inserted i
        WHERE dbo.CheckOverlappingContracts(i.EmployeeId, i.StartDate, i.EndDate, i.Id) = 1
    )
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 50000, 'Cannot create or update contract: Overlapping contract periods not allowed', 1;
    END
END;

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
    CONSTRAINT CK_Invoice_DaysWorked_Positive CHECK (DaysWorked > 0),
    CONSTRAINT CK_Invoice_TotalAmount_Positive CHECK (TotalAmount > 0)
);

-- Index for faster invoice lookups by employee
CREATE INDEX IX_Invoice_EmployeeId ON Invoices(EmployeeId);

-- This function will be used to check for overlapping invoices
CREATE FUNCTION dbo.CheckOverlappingInvoices (
    @EmployeeId INT, 
    @StartDate DATE,
    @EndDate DATE,
    @ExcludeInvoiceId INT = NULL
)
RETURNS BIT
AS
BEGIN
    DECLARE @HasOverlap BIT = 0;
    
    IF EXISTS (
        SELECT 1 FROM Invoices
        WHERE EmployeeId = @EmployeeId
        AND (@ExcludeInvoiceId IS NULL OR Id != @ExcludeInvoiceId)
        AND (
            (StartDate <= @EndDate AND EndDate >= @StartDate)
        )
    )
        SET @HasOverlap = 1;
        
    RETURN @HasOverlap;
END;

-- Trigger to prevent overlapping invoices for the same employee
CREATE TRIGGER TR_Prevent_Overlapping_Invoices
ON Invoices
AFTER INSERT, UPDATE
AS
BEGIN
    IF EXISTS (
        SELECT 1 FROM inserted i
        WHERE dbo.CheckOverlappingInvoices(i.EmployeeId, i.StartDate, i.EndDate, i.Id) = 1
    )
    BEGIN
        ROLLBACK TRANSACTION;
        THROW 50000, 'Cannot create or update invoice: Overlapping invoice periods not allowed', 1;
    END
END;

CREATE TABLE Users (
    Id INT PRIMARY KEY IDENTITY(1,1),
    Username NVARCHAR(50) NOT NULL,
    PasswordHash NVARCHAR(MAX) NOT NULL,
    EmployeeId INT NULL,
    IsInvoiceManager BIT NOT NULL DEFAULT 0,
    CONSTRAINT FK_User_Employee FOREIGN KEY (EmployeeId) REFERENCES Employees(Id),
    CONSTRAINT UQ_User_Username UNIQUE (Username)
);
```

## Domain Layer Implementation

### Value Objects

```csharp
// InvoiceSystem.Domain/ValueObjects/DateRange.cs
public class DateRange : ValueObject
{
    public DateTime Start { get; private set; }
    public DateTime End { get; private set; }
    
    private DateRange() { } // For EF Core
    
    public DateRange(DateTime start, DateTime end)
    {
        if (end < start)
            throw new DomainException("End date cannot be before start date");
            
        Start = start;
        End = end;
    }
    
    public bool Contains(DateTime date) => date >= Start && date <= End;
    
    public bool Contains(DateRange range) => 
        range.Start >= Start && range.End <= End;
    
    public bool Overlaps(DateRange range) => 
        Start <= range.End && End >= range.Start;
    
    public int DaysCount() => (End - Start).Days + 1;
    
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Start;
        yield return End;
    }
}

### Repository Implementations

```csharp
// InvoiceSystem.Infrastructure/Repositories/EmployeeRepository.cs
public class EmployeeRepository : IEmployeeRepository
{
    private readonly InvoiceSystemDbContext _context;
    
    public EmployeeRepository(InvoiceSystemDbContext context)
    {
        _context = context;
    }
    
    public async Task<Employee> GetByIdAsync(int id)
    {
        return await _context.Employees
            .Include(e => e.Contracts)
            .SingleOrDefaultAsync(e => e.Id == id);
    }
    
    public async Task<Employee> GetByIdentifierAsync(string identifier)
    {
        return await _context.Employees
            .Include(e => e.Contracts)
            .SingleOrDefaultAsync(e => e.EmployeeIdentifier == identifier);
    }
    
    public async Task<IEnumerable<Employee>> GetAllAsync()
    {
        return await _context.Employees.ToListAsync();
    }
    
    public async Task AddAsync(Employee employee)
    {
        await _context.Employees.AddAsync(employee);
    }
    
    public Task UpdateAsync(Employee employee)
    {
        _context.Entry(employee).State = EntityState.Modified;
        return Task.CompletedTask;
    }
}

// InvoiceSystem.Infrastructure/Repositories/ContractRepository.cs
public class ContractRepository : IContractRepository
{
    private readonly InvoiceSystemDbContext _context;
    
    public ContractRepository(InvoiceSystemDbContext context)
    {
        _context = context;
    }
    
    public async Task<Contract> GetByIdAsync(int id)
    {
        return await _context.Contracts.FindAsync(id);
    }
    
    public async Task<IEnumerable<Contract>> GetByEmployeeIdAsync(int employeeId)
    {
        return await _context.Contracts
            .Where(c => c.EmployeeId == employeeId)
            .OrderByDescending(c => c.DateRange.Start)
            .ToListAsync();
    }
    
    public async Task<Contract> GetValidContractForPeriodAsync(int employeeId, DateTime startDate, DateTime endDate)
    {
        return await _context.Contracts
            .SingleOrDefaultAsync(c => 
                c.EmployeeId == employeeId &&
                c.DateRange.Start <= startDate &&
                c.DateRange.End >= endDate);
    }
    
    public async Task AddAsync(Contract contract)
    {
        await _context.Contracts.AddAsync(contract);
    }
    
    public Task UpdateAsync(Contract contract)
    {
        _context.Entry(contract).State = EntityState.Modified;
        return Task.CompletedTask;
    }
}

// InvoiceSystem.Infrastructure/Repositories/InvoiceRepository.cs
public class InvoiceRepository : IInvoiceRepository
{
    private readonly InvoiceSystemDbContext _context;
    
    public InvoiceRepository(InvoiceSystemDbContext context)
    {
        _context = context;
    }
    
    public async Task<Invoice> GetByIdAsync(int id)
    {
        return await _context.Invoices
            .Include(i => i.Employee)
            .SingleOrDefaultAsync(i => i.Id == id);
    }
    
    public async Task<IEnumerable<Invoice>> GetByEmployeeIdAsync(int employeeId)
    {
        return await _context.Invoices
            .Where(i => i.EmployeeId == employeeId)
            .OrderByDescending(i => i.DateRange.Start)
            .ToListAsync();
    }
    
    public async Task<IEnumerable<Invoice>> GetAllAsync()
    {
        return await _context.Invoices
            .Include(i => i.Employee)
            .OrderByDescending(i => i.DateRange.Start)
            .ToListAsync();
    }
    
    public async Task<bool> ExistsOverlappingInvoiceAsync(
        int employeeId, 
        DateTime startDate, 
        DateTime endDate, 
        int? excludeInvoiceId = null)
    {
        var query = _context.Invoices
            .Where(i => i.EmployeeId == employeeId)
            .Where(i => 
                (i.DateRange.Start <= endDate && i.DateRange.End >= startDate));
                
        if (excludeInvoiceId.HasValue)
            query = query.Where(i => i.Id != excludeInvoiceId.Value);
            
        return await query.AnyAsync();
    }
    
    public async Task AddAsync(Invoice invoice)
    {
        await _context.Invoices.AddAsync(invoice);
    }
    
    public Task UpdateAsync(Invoice invoice)
    {
        _context.Entry(invoice).State = EntityState.Modified;
        return Task.CompletedTask;
    }
}

// InvoiceSystem.Infrastructure/Repositories/UnitOfWork.cs
public class UnitOfWork : IUnitOfWork
{
    private readonly InvoiceSystemDbContext _context;
    
    public IEmployeeRepository Employees { get; }
    public IContractRepository Contracts { get; }
    public IInvoiceRepository Invoices { get; }
    
    public UnitOfWork(InvoiceSystemDbContext context)
    {
        _context = context;
        Employees = new EmployeeRepository(context);
        Contracts = new ContractRepository(context);
        Invoices = new InvoiceRepository(context);
    }
    
    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }
}
```

### Authentication Implementation

```csharp
// InvoiceSystem.Infrastructure/Identity/User.cs
public class User
{
    public int Id { get; set; }
    public string Username { get; set; }
    public string PasswordHash { get; set; }
    public int? EmployeeId { get; set; }
    public Employee Employee { get; set; }
    public bool IsInvoiceManager { get; set; }
}

// InvoiceSystem.Infrastructure/Identity/IUserService.cs
public interface IUserService
{
    Task<User> AuthenticateAsync(string username, string password);
    Task<bool> IsInvoiceManagerAsync(int userId);
    Task<int?> GetEmployeeIdAsync(int userId);
}

public class UserService : IUserService
{
    private readonly InvoiceSystemDbContext _context;
    
    public UserService(InvoiceSystemDbContext context)
    {
        _context = context;
    }
    
    public async Task<User> AuthenticateAsync(string username, string password)
    {
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            return null;
            
        var user = await _context.Users
            .Include(u => u.Employee)
            .SingleOrDefaultAsync(u => u.Username == username);
            
        // For this example, we're using simple comparison
        // In production, use proper password hashing (BCrypt, etc.)
        if (user == null || user.PasswordHash != password)
            return null;
            
        return user;
    }
    
    public async Task<bool> IsInvoiceManagerAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        return user?.IsInvoiceManager ?? false;
    }
    
    public async Task<int?> GetEmployeeIdAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        return user?.EmployeeId;
    }
}

// InvoiceSystem.Infrastructure/Identity/AuthorizationService.cs
public interface IAuthorizationService
{
    Task<bool> CanViewInvoiceAsync(int userId, int invoiceId);
    Task<bool> CanCreateInvoiceAsync(int userId);
}

public class AuthorizationService : IAuthorizationService
{
    private readonly IUserService _userService;
    private readonly IUnitOfWork _unitOfWork;
    
    public AuthorizationService(IUserService userService, IUnitOfWork unitOfWork)
    {
        _userService = userService;
        _unitOfWork = unitOfWork;
    }
    
    public async Task<bool> CanViewInvoiceAsync(int userId, int invoiceId)
    {
        // Invoice managers can view all invoices
        if (await _userService.IsInvoiceManagerAsync(userId))
            return true;
            
        // Employees can only view their own invoices
        var employeeId = await _userService.GetEmployeeIdAsync(userId);
        if (!employeeId.HasValue)
            return false;
            
        var invoice = await _unitOfWork.Invoices.GetByIdAsync(invoiceId);
        return invoice != null && invoice.EmployeeId == employeeId.Value;
    }
    
    public async Task<bool> CanCreateInvoiceAsync(int userId)
    {
        // Only invoice managers can create invoices
        return await _userService.IsInvoiceManagerAsync(userId);
    }
}
```

### Database Initialization

```csharp
// InvoiceSystem.Infrastructure/Data/DbInitializer.cs
public static class DbInitializer
{
    public static async Task InitializeAsync(InvoiceSystemDbContext context)
    {
        // Make sure database is created
        await context.Database.EnsureCreatedAsync();
        
        // Return if there are already employees
        if (await context.Employees.AnyAsync())
            return;
            
        // Add employees
        var employees = new List<Employee>
        {
            new Employee("John", "Doe", "EMP001"),
            new Employee("Jane", "Smith", "EMP002"),
            new Employee("Mark", "Johnson", "EMP003")
        };
        
        await context.Employees.AddRangeAsync(employees);
        await context.SaveChangesAsync();
        
        // Add contracts
        var contracts = new List<Contract>
        {
            new Contract(DateTime.Now.AddMonths(-6), DateTime.Now.AddMonths(6), 100M) { EmployeeId = 1 },
            new Contract(DateTime.Now.AddMonths(-3), DateTime.Now.AddMonths(9), 120M) { EmployeeId = 2 },
            new Contract(DateTime.Now.AddMonths(-12), DateTime.Now.AddMonths(-6), 90M) { EmployeeId = 3 },
            new Contract(DateTime.Now.AddMonths(-6), DateTime.Now.AddMonths(6), 95M) { EmployeeId = 3 }
        };
        
        await context.Contracts.AddRangeAsync(contracts);
        await context.SaveChangesAsync();
        
        // Add users
        var users = new List<User>
        {
            new User { Username = "john", PasswordHash = "password123", EmployeeId = 1, IsInvoiceManager = false },
            new User { Username = "jane", PasswordHash = "password123", EmployeeId = 2, IsInvoiceManager = false },
            new User { Username = "mark", PasswordHash = "password123", EmployeeId = 3, IsInvoiceManager = false },
            new User { Username = "admin", PasswordHash = "admin123", IsInvoiceManager = true }
        };
        
        await context.Users.AddRangeAsync(users);
        await context.SaveChangesAsync();
    }
}
```

## Presentation Layer Implementation

### Controllers

```csharp
// InvoiceSystem.Web/Controllers/AccountController.cs
public class AccountController : Controller
{
    private readonly IUserService _userService;
    private readonly ILogger<AccountController> _logger;
    
    public AccountController(IUserService userService, ILogger<AccountController> logger)
    {
        _userService = userService;
        _logger = logger;
    }
    
    [HttpGet]
    public IActionResult Login()
    {
        if (User.Identity.IsAuthenticated)
            return RedirectToAction("Index", "Invoice");
            
        return View();
    }
    
    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);
            
        var user = await _userService.AuthenticateAsync(model.Username, model.Password);
        if (user == null)
        {
            _logger.LogWarning("Failed login attempt for user: {Username}", model.Username);
            ModelState.AddModelError("", "Invalid username or password");
            return View(model);
        }
        
        _logger.LogInformation("User logged in: {Username}", model.Username);
        
        // Set up authentication cookie
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
    private readonly IMediator _mediator;
    private readonly IAuthorizationService _authorizationService;
    private readonly ILogger<InvoiceController> _logger;
    
    public InvoiceController(
        IMediator mediator,
        IAuthorizationService authorizationService,
        ILogger<InvoiceController> logger)
    {
        _mediator = mediator;
        _authorizationService = authorizationService;
        _logger = logger;
    }
    
    public async Task<IActionResult> Index()
    {
        int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
        
        try
        {
            GetInvoicesQuery query = new GetInvoicesQuery();
            
            // If user is not an invoice manager, restrict to their own invoices
            if (!User.IsInRole("InvoiceManager"))
            {
                var employeeId = User.FindFirstValue("EmployeeId");
                if (string.IsNullOrEmpty(employeeId))
                    return Forbid();
                    
                query.EmployeeId = int.Parse(employeeId);
            }
            
            var result = await _mediator.Send(query);
            
            if (!result.IsSuccess)
            {
                _logger.LogError("Error retrieving invoices: {Error}", result.Error);
                TempData["ErrorMessage"] = result.Error;
                return View(new List<InvoiceDto>());
            }
            
            return View(result.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Invoice/Index");
            TempData["ErrorMessage"] = "An error occurred while retrieving invoices.";
            return View(new List<InvoiceDto>());
        }
    }
    
    [HttpGet]
    [Authorize(Roles = "InvoiceManager")]
    public async Task<IActionResult> Create()
    {
        try
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            
            if (!await _authorizationService.CanCreateInvoiceAsync(userId))
                return Forbid();
                
            var employees = await _mediator.Send(new GetEmployeesQuery());
            
            var model = new CreateInvoiceViewModel
            {
                StartDate = DateTime.Today,
                EndDate = DateTime.Today.AddDays(30)
            };
            
            ViewBag.Employees = new SelectList(employees.Value, "Id", "FullName");
            
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Invoice/Create");
            TempData["ErrorMessage"] = "An error occurred while preparing the invoice form.";
            return RedirectToAction(nameof(Index));
        }
    }
    
    [HttpPost]
    [Authorize(Roles = "InvoiceManager")]
    public async Task<IActionResult> Create(CreateInvoiceViewModel model)
    {
        try
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            
            if (!await _authorizationService.CanCreateInvoiceAsync(userId))
                return Forbid();
                
            if (!ModelState.IsValid)
            {
                var employees = await _mediator.Send(new GetEmployeesQuery());
                ViewBag.Employees = new SelectList(employees.Value, "Id", "FullName");
                return View(model);
            }
            
            var command = new CreateInvoiceCommand
            {
                EmployeeId = model.EmployeeId,
                StartDate = model.StartDate,
                EndDate = model.EndDate,
                DaysWorked = model.DaysWorked
            };
            
            var result = await _mediator.Send(command);
            
            if (!result.IsSuccess)
            {
                _logger.LogWarning("Invoice creation failed: {Error}", result.Error);
                ModelState.AddModelError("", result.Error);
                var employees = await _mediator.Send(new GetEmployeesQuery());
                ViewBag.Employees = new SelectList(employees.Value, "Id", "FullName");
                return View(model);
            }
            
            _logger.LogInformation("Invoice created successfully. ID: {InvoiceId}", result.Value);
            TempData["SuccessMessage"] = "Invoice created successfully.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Invoice/Create POST");
            ModelState.AddModelError("", "An error occurred while creating the invoice.");
            var employees = await _mediator.Send(new GetEmployeesQuery());
            ViewBag.Employees = new SelectList(employees.Value, "Id", "FullName");
            return View(model);
        }
    }
    
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        try
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            
            if (!await _authorizationService.CanViewInvoiceAsync(userId, id))
                return Forbid();
                
            var result = await _mediator.Send(new GetInvoiceByIdQuery { Id = id });
            
            if (!result.IsSuccess)
            {
                _logger.LogWarning("Invoice details retrieval failed: {Error}", result.Error);
                TempData["ErrorMessage"] = result.Error;
                return RedirectToAction(nameof(Index));
            }
            
            return View(result.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Invoice/Details");
            TempData["ErrorMessage"] = "An error occurred while retrieving invoice details.";
            return RedirectToAction(nameof(Index));
        }
    }
}
```

### ViewModels

```csharp
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
    [InvoiceDateRange(ErrorMessage = "End date must be after or equal to Start date")]
    public DateTime EndDate { get; set; }
    
    [Required]
    [Display(Name = "Days Worked")]
    [Range(1, 366, ErrorMessage = "Days worked must be between 1 and 366")]
    public int DaysWorked { get; set; }
}

// InvoiceSystem.Web/Validation/InvoiceDateRangeAttribute.cs
public class InvoiceDateRangeAttribute : ValidationAttribute
{
    protected override ValidationResult IsValid(object value, ValidationContext validationContext)
    {
        var model = (CreateInvoiceViewModel)validationContext.ObjectInstance;
        
        if (model.EndDate < model.StartDate)
        {
            return new ValidationResult(ErrorMessageString);
        }
        
        // Check if days worked fits within the date range
        int maxPossibleDays = (model.EndDate - model.StartDate).Days + 1;
        if (model.DaysWorked > maxPossibleDays)
        {
            return new ValidationResult($"Days worked ({model.DaysWorked}) cannot exceed the maximum possible days in this period ({maxPossibleDays})");
        }
        
        return ValidationResult.Success;
    }
}
```

### Views

```html
<!-- InvoiceSystem.Web/Views/Account/Login.cshtml -->
@model LoginViewModel

@{
    ViewData["Title"] = "Login";
}

<div class="row justify-content-center">
    <div class="col-md-6">
        <div class="card">
            <div class="card-header">
                <h2>Login</h2>
            </div>
            <div class="card-body">
                <form asp-action="Login" method="post">
                    <div asp-validation-summary="ModelOnly" class="text-danger"></div>
                    
                    <div class="form-group mb-3">
                        <label asp-for="Username" class="control-label"></label>
                        <input asp-for="Username" class="form-control" />
                        <span asp-validation-for="Username" class="text-danger"></span>
                    </div>
                    
                    <div class="form-group mb-3">
                        <label asp-for="Password" class="control-label"></label>
                        <input asp-for="Password" class="form-control" />
                        <span asp-validation-for="Password" class="text-danger"></span>
                    </div>
                    
                    <div class="form-group">
                        <button type="submit" class="btn btn-primary">Login</button>
                    </div>
                </form>
            </div>
        </div>
    </div>
</div>

<!-- InvoiceSystem.Web/Views/Invoice/Index.cshtml -->
@model IEnumerable<InvoiceDto>

@{
    ViewData["Title"] = "Invoices";
}

<h1>Invoices</h1>

@if (TempData["SuccessMessage"] != null)
{
    <div class="alert alert-success">
        @TempData["SuccessMessage"]
    </div>
}

@if (TempData["ErrorMessage"] != null)
{
    <div class="alert alert-danger">
        @TempData["ErrorMessage"]
    </div>
}

@if (User.IsInRole("InvoiceManager"))
{
    <p>
        <a asp-action="Create" class="btn btn-primary">Create New Invoice</a>
    </p>
}

<table class="table table-striped">
    <thead>
        <tr>
            <th>
                @Html.DisplayNameFor(model => model.Id)
            </th>
            <th>
                @Html.DisplayNameFor(model => model.EmployeeName)
            </th>
            <th>
                @Html.DisplayNameFor(model => model.StartDate)
            </th>
            <th>
                @Html.DisplayNameFor(model => model.EndDate)
            </th>
            <th>
                @Html.DisplayNameFor(model => model.DaysWorked)
            </th>
            <th>
                @Html.DisplayNameFor(model => model.TotalAmount)
            </th>
            <th>Actions</th>
        </tr>
    </thead>
    <tbody>
        @foreach (var item in Model)
        {
            <tr>
                <td>
                    @Html.DisplayFor(modelItem => item.Id)
                </td>
                <td>
                    @Html.DisplayFor(modelItem => item.EmployeeName)
                </td>
                <td>
                    @item.StartDate.ToShortDateString()
                </td>
                <td>
                    @item.EndDate.ToShortDateString()
                </td>
                <td>
                    @Html.DisplayFor(modelItem => item.DaysWorked)
                </td>
                <td>
                    $@Html.DisplayFor(modelItem => item.TotalAmount)
                </td>
                <td>
                    <a asp-action="Details" asp-route-id="@item.Id" class="btn btn-info btn-sm">Details</a>
                </td>
            </tr>
        }
    </tbody>
</table>

<!-- InvoiceSystem.Web/Views/Invoice/Create.cshtml -->
@model CreateInvoiceViewModel

@{
    ViewData["Title"] = "Create Invoice";
}

<h1>Create Invoice</h1>

<div class="row">
    <div class="col-md-8">
        <div class="card">
            <div class="card-header">
                <h3>New Invoice</h3>
            </div>
            <div class="card-body">
                <form asp-action="Create" method="post">
                    <div asp-validation-summary="ModelOnly" class="text-danger"></div>
                    
                    <div class="form-group mb-3">
                        <label asp-for="EmployeeId" class="control-label"></label>
                        <select asp-for="EmployeeId" class="form-control" asp-items="ViewBag.Employees">
                            <option value="">-- Select Employee --</option>
                        </select>
                        <span asp-validation-for="EmployeeId" class="text-danger"></span>
                    </div>
                    
                    <div class="form-group mb-3">
                        <label asp-for="StartDate" class="control-label"></label>
                        <input asp-for="StartDate" class="form-control" />
                        <span asp-validation-for="StartDate" class="text-danger"></span>
                    </div>
                    
                    <div class="form-group mb-3">
                        <label asp-for="EndDate" class="control-label"></label>
                        <input asp-for="EndDate" class="form-control" />
                        <span asp-validation-for="EndDate" class="text-danger"></span>
                    </div>
                    
                    <div class="form-group mb-3">
                        <label asp-for="DaysWorked" class="control-label"></label>
                        <input asp-for="DaysWorked" class="form-control" />
                        <span asp-validation-for="DaysWorked" class="text-danger"></span>
                    </div>
                    
                    <div class="form-group">
                        <button type="submit" class="btn btn-primary">Create</button>
                        <a asp-action="Index" class="btn btn-secondary">Back to List</a>
                    </div>
                </form>
            </div>
        </div>
    </div>
    <div class="col-md-4">
        <div class="card">
            <div class="card-header">
                <h3>Validation Rules</h3>
            </div>
            <div class="card-body">
                <ul>
                    <li>The employee must have a valid contract for the entire invoice period.</li>
                    <li>The number of days worked must not exceed the invoice period.</li>
                    <li>The employee cannot have overlapping invoices.</li>
                    <li>The total amount is calculated automatically based on the contract's daily rate.</li>
                </ul>
            </div>
        </div>
    </div>
</div>

<!-- InvoiceSystem.Web/Views/Invoice/Details.cshtml -->
@model InvoiceDto

@{
    ViewData["Title"] = "Invoice Details";
}

<h1>Invoice Details</h1>

<div class="card">
    <div class="card-header">
        <h3>Invoice #@Model.Id</h3>
    </div>
    <div class="card-body">
        <dl class="row">
            <dt class="col-sm-3">Employee:</dt>
            <dd class="col-sm-9">@Model.EmployeeName</dd>
            
            <dt class="col-sm-3">Invoice Period:</dt>
            <dd class="col-sm-9">@Model.StartDate.ToShortDateString() - @Model.EndDate.ToShortDateString()</dd>
            
            <dt class="col-sm-3">Days Worked:</dt>
            <dd class="col-sm-9">@Model.DaysWorked</dd>
            
            <dt class="col-sm-3">Total Amount:</dt>
            <dd class="col-sm-9">$@Model.TotalAmount</dd>
            
            <dt class="col-sm-3">Created At:</dt>
            <dd class="col-sm-9">@Model.CreatedAt</dd>
        </dl>
    </div>
    <div class="card-footer">
        <a asp-action="Index" class="btn btn-primary">Back to List</a>
    </div>
</div>
```

## Configuration and Dependency Injection

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Add services to container
builder.Services.AddDbContext<InvoiceSystemDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register MediatR
builder.Services.AddMediatR(typeof(CreateInvoiceCommand).Assembly);

// Register repositories and domain services
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IInvoiceValidationService, InvoiceValidationService>();

// Register application services
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IAuthorizationService, AuthorizationService>();

// Add authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
    });

builder.Services.AddControllersWithViews();

// Add logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var app = builder.Build();

// Configure middleware
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    
    // Seed database in development
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        var context = services.GetRequiredService<InvoiceSystemDbContext>();
        DbInitializer.InitializeAsync(context).Wait();
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
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
```

## Unit Tests

```csharp
// InvoiceSystem.UnitTests/Domain/EmployeeTests.cs
public class EmployeeTests
{
    [Fact]
    public void AddContract_WithValidContract_AddsContract()
    {
        // Arrange
        var employee = new Employee("John", "Doe", "EMP001");
        var contract = new Contract(
            new DateRange(DateTime.Today, DateTime.Today.AddMonths(6)),
            new Money(100M));
            
        // Act
        employee.AddContract(contract);
            
        // Assert
        Assert.Single(employee.Contracts);
        Assert.Contains(contract, employee.Contracts);
    }
    
    [Fact]
    public void AddContract_WithOverlappingContract_ThrowsException()
    {
        // Arrange
        var employee = new Employee("John", "Doe", "EMP001");
        var contract1 = new Contract(
            new DateRange(DateTime.Today, DateTime.Today.AddMonths(6)),
            new Money(100M));
            
        var contract2 = new Contract(
            new DateRange(DateTime.Today.AddMonths(3), DateTime.Today.AddMonths(9)),
            new Money(120M));
            
        employee.AddContract(contract1);
            
        // Act & Assert
        var exception = Assert.Throws<DomainException>(() => employee.AddContract(contract2));
        Assert.Contains("overlap", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
    
    [Fact]
    public void GetValidContractForPeriod_WithValidContract_ReturnsContract()
    {
        // Arrange
        var employee = new Employee("John", "Doe", "EMP001");
        var contractStart = DateTime.Today;
        var contractEnd = DateTime.Today.AddMonths(6);
        
        var contract = new Contract(
            new DateRange(contractStart, contractEnd),
            new Money(100M));
            
        employee.AddContract(contract);
            
        // Invoice period is within contract period
        var invoicePeriod = new DateRange(
            contractStart.AddMonths(1),
            contractStart.AddMonths(2));
            
        // Act
        var result = employee.GetValidContractForPeriod(invoicePeriod);
            
        // Assert
        Assert.Equal(contract, result);
    }
    
    [Fact]
    public void GetValidContractForPeriod_WithNoValidContract_ReturnsNull()
    {
        // Arrange
        var employee = new Employee("John", "Doe", "EMP001");
        var contractStart = DateTime.Today;
        var contractEnd = DateTime.Today.AddMonths(6);
        
        var contract = new Contract(
            new DateRange(contractStart, contractEnd),
            new Money(100M));
            
        employee.AddContract(contract);
            
        // Invoice period extends beyond contract period
        var invoicePeriod = new DateRange(
            contractStart.AddMonths(1),
            contractEnd.AddMonths(1));
            
        // Act
        var result = employee.GetValidContractForPeriod(invoicePeriod);
            
        // Assert
        Assert.Null(result);
    }
}

// InvoiceSystem.UnitTests/Domain/InvoiceTests.cs
public class InvoiceTests
{
    [Fact]
    public void Create_WithValidParameters_CreatesInvoice()
    {
        // Arrange
        var employee = new Employee("John", "Doe", "EMP001");
        var contract = new Contract(
            new DateRange(DateTime.Today.AddMonths(-1), DateTime.Today.AddMonths(5)),
            new Money(100M));
            
        employee.AddContract(contract);
        
        var startDate = DateTime.Today;
        var endDate = DateTime.Today.AddDays(10);
        var daysWorked = 11; // 11 days (inclusive of start and end)
        
        // Act
        var invoice = Invoice.Create(employee, new DateRange(startDate, endDate), daysWorked);
        
        // Assert
        Assert.Equal(employee.Id, invoice.EmployeeId);
        Assert.Equal(startDate, invoice.DateRange.Start);
        Assert.Equal(endDate, invoice.DateRange.End);
        Assert.Equal(daysWorked, invoice.DaysWorked);
        Assert.Equal(100M * daysWorked, invoice.TotalAmount.Amount);
    }
    
    [Fact]
    public void Create_WithTooManyDaysWorked_ThrowsException()
    {
        // Arrange
        var employee = new Employee("John", "Doe", "EMP001");
        var contract = new Contract(
            new DateRange(DateTime.Today.AddMonths(-1), DateTime.Today.AddMonths(5)),
            new Money(100M));
            
        employee.AddContract(contract);
        
        var startDate = DateTime.Today;
        var endDate = DateTime.Today.AddDays(10);
        var daysWorked = 12; // Too many for a 11-day period (inclusive)
        
        // Act & Assert
        var exception = Assert.Throws<DomainException>(() => 
            Invoice.Create(employee, new DateRange(startDate, endDate), daysWorked));
            
        Assert.Contains("days worked", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
    
    [Fact]
    public void Create_WithNoValidContract_ThrowsException()
    {
        // Arrange
        var employee = new Employee("John", "Doe", "EMP001");
        var contract = new Contract(
            new DateRange(DateTime.Today.AddMonths(-6), DateTime.Today.AddMonths(-1)),
            new Money(100M));
            
        employee.AddContract(contract);
        
        var startDate = DateTime.Today;
        var endDate = DateTime.Today.AddDays(10);
        var daysWorked = 11;
        
        // Act & Assert
        var exception = Assert.Throws<DomainException>(() => 
            Invoice.Create(employee, new DateRange(startDate, endDate), daysWorked));
            
        Assert.Contains("contract", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
}

// InvoiceSystem.UnitTests/Application/CreateInvoiceCommandHandlerTests.cs
public class CreateInvoiceCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IInvoiceValidationService> _mockValidationService;
    private readonly CreateInvoiceCommandHandler _handler;
    
    public CreateInvoiceCommandHandlerTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockValidationService = new Mock<IInvoiceValidationService>();
        _handler = new CreateInvoiceCommandHandler(_mockUnitOfWork.Object, _mockValidationService.Object);
    }
    
    [Fact]
    public async Task Handle_WithValidCommand_CreatesInvoice()
    {
        // Arrange
        var employee = new Employee("John", "Doe", "EMP001");
        var contract = new Contract(
            new DateRange(DateTime.Today.AddMonths(-1), DateTime.Today.AddMonths(5)),
            new Money(100M));
            
        employee.AddContract(contract);
        
        var command = new CreateInvoiceCommand
        {
            EmployeeId = 1,
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddDays(10),
            DaysWorked = 11
        };
        
        _mockUnitOfWork.Setup(u => u.Employees.GetByIdAsync(1))
            .ReturnsAsync(employee);
            
        _mockUnitOfWork.Setup(u => u.Invoices.AddAsync(It.IsAny<Invoice>()))
            .Returns(Task.CompletedTask);
            
        _mockUnitOfWork.Setup(u => u.SaveChangesAsync())
            .ReturnsAsync(1);
            
        // Act
        var result = await _handler.Handle(command, CancellationToken.None);
        
        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotEqual(0, result.Value);
        
        _mockUnitOfWork.Verify(u => u.Invoices.AddAsync(It.IsAny<Invoice>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }
    
    [Fact]
    public async Task Handle_WithValidationError_ReturnsFailure()
    {
        // Arrange
        var employee = new Employee("John", "Doe", "EMP001");
        var contract = new Contract(
            new DateRange(DateTime.Today.AddMonths(-1), DateTime.Today.AddMonths(5)),
            new Money(100M));
            
        employee.AddContract(contract);
        
        var command = new CreateInvoiceCommand
        {
            EmployeeId = 1,
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddDays(10),
            DaysWorked = 11
        };
        
        _mockUnitOfWork.Setup(u => u.Employees.GetByIdAsync(1))
            .ReturnsAsync(employee);
            
        _mockValidationService.Setup(v => v.ValidateInvoiceAsync(
                It.IsAny<Employee>(), 
                It.IsAny<DateRange>(), 
                It.IsAny<int>(), 
                It.IsAny<decimal>()))
            .Throws(new DomainException("Validation error"));
            
        // Act
        var result = await _handler.Handle(command, CancellationToken.None);
        
        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Validation error", result.Error);
        
        _mockUnitOfWork.Verify(u => u.Invoices.AddAsync(It.IsAny<Invoice>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
    }
}
```

## Integration Tests

```csharp
// InvoiceSystem.IntegrationTests/Repositories/InvoiceRepositoryTests.cs
public class InvoiceRepositoryTests : IClassFixture<TestDatabaseFixture>
{
    private readonly TestDatabaseFixture _fixture;
    
    public InvoiceRepositoryTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
    }
    
    [Fact]
    public async Task ExistsOverlappingInvoiceAsync_WithOverlappingInvoice_ReturnsTrue()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var repository = new InvoiceRepository(context);
        
        var employee = new Employee("Test", "User", "TEST001");
        context.Employees.Add(employee);
        await context.SaveChangesAsync();
        
        var contract = new Contract(
            DateTime.Today.AddMonths(-1),
            DateTime.Today.AddMonths(5),
            100M)
        {
            EmployeeId = employee.Id
        };
        context.Contracts.Add(contract);
        await context.SaveChangesAsync();
        
        var invoice = Invoice.Create(
            employee,
            new DateRange(DateTime.Today, DateTime.Today.AddDays(10)),
            5);
        context.Invoices.Add(invoice);
        await context.SaveChangesAsync();
        
        // Overlapping period
        var startDate = DateTime.Today.AddDays(5);
        var endDate = DateTime.Today.AddDays(15);
        
        // Act
        var result = await repository.ExistsOverlappingInvoiceAsync(
            employee.Id, startDate, endDate);
        
        // Assert
        Assert.True(result);
    }
    
    [Fact]
    public async Task ExistsOverlappingInvoiceAsync_WithNonOverlappingInvoice_ReturnsFalse()
    {
        // Arrange
        using var context = _fixture.CreateContext();
        var repository = new InvoiceRepository(context);
        
        var employee = new Employee("Test", "User", "TEST002");
        context.Employees.Add(employee);
        await context.SaveChangesAsync();
        
        var contract = new Contract(
            DateTime.Today.AddMonths(-1),
            DateTime.Today.AddMonths(5),
            100M)
        {
            EmployeeId = employee.Id
        };
        context.Contracts.Add(contract);
        await context.SaveChangesAsync();
        
        var invoice = Invoice.Create(
            employee,
            new DateRange(DateTime.Today, DateTime.Today.AddDays(10)),
            5);
        context.Invoices.Add(invoice);
        await context.SaveChangesAsync();
        
        // Non-overlapping period
        var startDate = DateTime.Today.AddDays(11);
        var endDate = DateTime.Today.AddDays(20);
        
        // Act
        var result = await repository.ExistsOverlappingInvoiceAsync(
            employee.Id, startDate, endDate);
        
        // Assert
        Assert.False(result);
    }
}

// InvoiceSystem.IntegrationTests/TestDatabaseFixture.cs
public class TestDatabaseFixture
{
    private const string ConnectionString = "Server=(localdb)\\mssqllocaldb;Database=InvoiceSystemTest;Trusted_Connection=True;MultipleActiveResultSets=true";
    private static readonly object _lock = new object();
    private static bool _databaseInitialized;
    
    public TestDatabaseFixture()
    {
        lock (_lock)
        {
            if (!_databaseInitialized)
            {
                using (var context = CreateContext())
                {
                    context.Database.EnsureDeleted();
                    context.Database.EnsureCreated();
                    
                    // Seed with any test data if needed
                }
                
                _databaseInitialized = true;
            }
        }
    }
    
    public InvoiceSystemDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<InvoiceSystemDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;
            
        return new InvoiceSystemDbContext(options);
    }
}
```

## Prototype Implementation Summary

This implementation provides a robust foundation for the invoice system that meets all of the requirements (R1-R10) stated in the original document. Key improvements include:

1. **Rich Domain Model**
   - Value objects (DateRange, Money) for cleaner domain modeling
   - Encapsulated collections with proper access control
   - Domain entity validations in constructors and factory methods

2. **Enhanced Validation**
   - Domain-level validation for business rules
   - DB-level constraints through check constraints and triggers
   - Application-level validation in command handlers
   - UI-level validation with custom validation attributes

3. **Improved Architecture**
   - Clean separation of concerns across layers
   - CQRS pattern for clear distinction between read and write operations
   - Repository pattern with Unit of Work for data access abstraction
   - Proper error handling and logging throughout the application

4. **Security and Authorization**
   - Dedicated authorization service for fine-grained access control
   - Role-based permissions for invoice managers vs. employees
   - Clean authentication with claims-based identity

5. **Comprehensive Testing**
   - Unit tests for domain logic and business rules
   - Integration tests for repository functionality
   - Mock-based tests for application services

6. **User Experience**
   - Clear validation messages for invoice creation
   - Role-specific UI elements
   - Informative error and success messages

This implementation demonstrates solid software design principles and addresses the specific business requirements while maintaining clean, maintainable code.
public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
        builder.Property(e => e.LastName).IsRequired().HasMaxLength(100);
        builder.Property(e => e.EmployeeIdentifier).IsRequired().HasMaxLength(50);
        
        builder.HasIndex(e => e.EmployeeIdentifier).IsUnique();
        
        // Enforce that an employee has only one active contract at a time (R5)
        // This is handled in domain logic but DB helps enforce this too
        builder.HasMany(e => e.Contracts)
               .WithOne()
               .HasForeignKey("EmployeeId")
               .OnDelete(DeleteBehavior.Cascade);
               
        builder.HasMany(e => e.Invoices)
               .WithOne()
               .HasForeignKey("EmployeeId")
               .OnDelete(DeleteBehavior.Cascade);
    }
}

// InvoiceSystem.Domain/ValueObjects/Money.cs
public class Money : ValueObject
{
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = "USD"; // Default currency
    
    private Money() { } // For EF Core
    
    public Money(decimal amount, string currency = "USD")
    {
        if (amount < 0)
            throw new DomainException("Money amount cannot be negative");
            
        Amount = amount;
        Currency = currency;
    }
    
    public static Money operator *(Money money, int multiplier)
    {
        return new Money(money.Amount * multiplier, money.Currency);
    }
    
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }
}

// InvoiceSystem.Domain/ValueObjects/ValueObject.cs
public abstract class ValueObject
{
    protected abstract IEnumerable<object> GetEqualityComponents();
    
    public override bool Equals(object obj)
    {
        if (obj == null || obj.GetType() != GetType())
        {
            return false;
        }
        
        var other = (ValueObject)obj;
        
        return GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
    }
    
    public override int GetHashCode()
    {
        return GetEqualityComponents()
            .Select(x => x != null ? x.GetHashCode() : 0)
            .Aggregate((x, y) => x ^ y);
    }
    
    public static bool operator ==(ValueObject left, ValueObject right)
    {
        if (ReferenceEquals(left, null) && ReferenceEquals(right, null))
            return true;
            
        if (ReferenceEquals(left, null) || ReferenceEquals(right, null))
            return false;
            
        return left.Equals(right);
    }
    
    public static bool operator !=(ValueObject left, ValueObject right)
    {
        return !(left == right);
    }
}
```

### Domain Entities

```csharp
// InvoiceSystem.Domain/Entities/Employee.cs
public class Employee : AggregateRoot
{
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public string EmployeeIdentifier { get; private set; }
    
    private readonly List<Contract> _contracts = new List<Contract>();
    public IReadOnlyCollection<Contract> Contracts => _contracts.AsReadOnly();
    
    private readonly List<Invoice> _invoices = new List<Invoice>();
    public IReadOnlyCollection<Invoice> Invoices => _invoices.AsReadOnly();
    
    private Employee() { } // For EF Core
    
    public Employee(string firstName, string lastName, string employeeIdentifier)
    {
        if (string.IsNullOrWhiteSpace(firstName))
            throw new DomainException("First name cannot be empty");
            
        if (string.IsNullOrWhiteSpace(lastName))
            throw new DomainException("Last name cannot be empty");
            
        if (string.IsNullOrWhiteSpace(employeeIdentifier))
            throw new DomainException("Employee identifier cannot be empty");
            
        FirstName = firstName;
        LastName = lastName;
        EmployeeIdentifier = employeeIdentifier;
    }
    
    public string FullName => $"{FirstName} {LastName}";
    
    public void AddContract(Contract contract)
    {
        if (contract == null)
            throw new DomainException("Contract cannot be null");
            
        if (_contracts.Any(c => c.DateRange.Overlaps(contract.DateRange)))
            throw new DomainException("Contract periods cannot overlap");
            
        _contracts.Add(contract);
    }
    
    public Contract GetValidContractForPeriod(DateRange dateRange)
    {
        return _contracts.SingleOrDefault(c => c.DateRange.Contains(dateRange));
    }
    
    public void AddInvoice(Invoice invoice)
    {
        if (invoice == null)
            throw new DomainException("Invoice cannot be null");
            
        _invoices.Add(invoice);
    }
    
    public bool HasOverlappingInvoice(DateRange dateRange, int? excludeInvoiceId = null)
    {
        return _invoices.Any(i => 
            i.Id != excludeInvoiceId && 
            i.DateRange.Overlaps(dateRange));
    }
}

// InvoiceSystem.Domain/Entities/Contract.cs
public class Contract : Entity
{
    public int EmployeeId { get; private set; }
    public DateRange DateRange { get; private set; }
    public Money DailyRate { get; private set; }
    
    private Contract() { } // For EF Core
    
    public Contract(DateRange dateRange, Money dailyRate)
    {
        DateRange = dateRange ?? throw new DomainException("Date range cannot be null");
        DailyRate = dailyRate ?? throw new DomainException("Daily rate cannot be null");
    }
    
    // Helper constructor with primitive types for easier creation
    public Contract(DateTime startDate, DateTime endDate, decimal dailyRate)
        : this(new DateRange(startDate, endDate), new Money(dailyRate))
    {
    }
    
    public void SetEmployee(int employeeId)
    {
        if (employeeId <= 0)
            throw new DomainException("Employee ID must be positive");
            
        EmployeeId = employeeId;
    }
}

// InvoiceSystem.Domain/Entities/Invoice.cs
public class Invoice : Entity
{
    public int EmployeeId { get; private set; }
    public DateRange DateRange { get; private set; }
    public int DaysWorked { get; private set; }
    public Money TotalAmount { get; private set; }
    public DateTime CreatedAt { get; private set; }
    
    private Invoice() { } // For EF Core
    
    private Invoice(int employeeId, DateRange dateRange, int daysWorked, Money totalAmount)
    {
        EmployeeId = employeeId;
        DateRange = dateRange ?? throw new DomainException("Date range cannot be null");
        DaysWorked = daysWorked;
        TotalAmount = totalAmount ?? throw new DomainException("Total amount cannot be null");
        CreatedAt = DateTime.UtcNow;
    }
    
    // Factory method to create invoice with validation
    public static Invoice Create(Employee employee, DateRange dateRange, int daysWorked)
    {
        if (employee == null)
            throw new DomainException("Employee cannot be null");
            
        // Validate days worked is within the range
        if (daysWorked <= 0)
            throw new DomainException("Days worked must be positive");
            
        if (daysWorked > dateRange.DaysCount())
            throw new DomainException($"Days worked ({daysWorked}) cannot exceed invoice period days ({dateRange.DaysCount()})");
            
        // Find valid contract for this period
        var contract = employee.GetValidContractForPeriod(dateRange);
        if (contract == null)
            throw new DomainException("No valid contract exists for this invoice period");
            
        // Calculate total amount
        var totalAmount = contract.DailyRate * daysWorked;
        
        return new Invoice(employee.Id, dateRange, daysWorked, totalAmount);
    }
    
    // Helper method to create invoice with primitive types
    public static Invoice Create(Employee employee, DateTime startDate, DateTime endDate, int daysWorked)
    {
        return Create(employee, new DateRange(startDate, endDate), daysWorked);
    }
}

// InvoiceSystem.Domain/Entities/Entity.cs
public abstract class Entity
{
    public int Id { get; protected set; }
    
    public override bool Equals(object obj)
    {
        var other = obj as Entity;
        
        if (ReferenceEquals(other, null))
            return false;
        
        if (ReferenceEquals(this, other))
            return true;
            
        if (GetType() != other.GetType())
            return false;
            
        if (Id == 0 || other.Id == 0)
            return false;
            
        return Id == other.Id;
    }
    
    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }
    
    public static bool operator ==(Entity left, Entity right)
    {
        if (ReferenceEquals(left, null) && ReferenceEquals(right, null))
            return true;
            
        if (ReferenceEquals(left, null) || ReferenceEquals(right, null))
            return false;
            
        return left.Equals(right);
    }
    
    public static bool operator !=(Entity left, Entity right)
    {
        return !(left == right);
    }
}

// InvoiceSystem.Domain/Entities/AggregateRoot.cs
public abstract class AggregateRoot : Entity
{
    // Additional functionality for aggregates can be added here
}
```

### Domain Exceptions

```csharp
// InvoiceSystem.Domain/Exceptions/DomainException.cs
public class DomainException : Exception
{
    public DomainException(string message) : base(message)
    {
    }
}
```

### Domain Services

```csharp
// InvoiceSystem.Domain/Services/IInvoiceValidationService.cs
public interface IInvoiceValidationService
{
    Task ValidateInvoiceAsync(Employee employee, DateRange dateRange, int daysWorked, decimal expectedAmount);
    Task<IEnumerable<string>> GetValidationErrorsAsync(Employee employee, DateRange dateRange, int daysWorked, decimal expectedAmount);
}

// InvoiceSystem.Domain/Services/InvoiceValidationService.cs
public class InvoiceValidationService : IInvoiceValidationService
{
    private readonly IUnitOfWork _unitOfWork;
    
    public InvoiceValidationService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }
    
    public async Task ValidateInvoiceAsync(Employee employee, DateRange dateRange, int daysWorked, decimal expectedAmount)
    {
        var errors = await GetValidationErrorsAsync(employee, dateRange, daysWorked, expectedAmount);
        if (errors.Any())
            throw new DomainException(string.Join("; ", errors));
    }
    
    public async Task<IEnumerable<string>> GetValidationErrorsAsync(Employee employee, DateRange dateRange, int daysWorked, decimal expectedAmount)
    {
        var errors = new List<string>();
        
        if (employee == null)
        {
            errors.Add("Employee cannot be null");
            return errors;
        }
        
        // Check if employee has an overlapping invoice
        if (await _unitOfWork.Invoices.ExistsOverlappingInvoiceAsync(employee.Id, dateRange.Start, dateRange.End))
            errors.Add("Employee already has an invoice for this period");
            
        // Check if employee has a valid contract for this period
        var contract = employee.GetValidContractForPeriod(dateRange);
        if (contract == null)
            errors.Add("No valid contract exists for this invoice period");
        else
        {
            // Calculate expected invoice amount
            decimal calculatedAmount = contract.DailyRate.Amount * daysWorked;
            if (calculatedAmount != expectedAmount)
                errors.Add($"Invoice amount {expectedAmount} does not match expected amount {calculatedAmount}");
        }
        
        // Check if days worked fits within the invoice period
        int maxDays = dateRange.DaysCount();
        if (daysWorked > maxDays)
            errors.Add($"Days worked ({daysWorked}) cannot exceed invoice period days ({maxDays})");
        
        return errors;
    }
}
```

### Domain Repository Interfaces

```csharp
// InvoiceSystem.Domain/Interfaces/IEmployeeRepository.cs
public interface IEmployeeRepository
{
    Task<Employee> GetByIdAsync(int id);
    Task<Employee> GetByIdentifierAsync(string identifier);
    Task<IEnumerable<Employee>> GetAllAsync();
    Task AddAsync(Employee employee);
    Task UpdateAsync(Employee employee);
}

// InvoiceSystem.Domain/Interfaces/IContractRepository.cs
public interface IContractRepository
{
    Task<Contract> GetByIdAsync(int id);
    Task<IEnumerable<Contract>> GetByEmployeeIdAsync(int employeeId);
    Task<Contract> GetValidContractForPeriodAsync(int employeeId, DateTime startDate, DateTime endDate);
    Task AddAsync(Contract contract);
    Task UpdateAsync(Contract contract);
}

// InvoiceSystem.Domain/Interfaces/IInvoiceRepository.cs
public interface IInvoiceRepository
{
    Task<Invoice> GetByIdAsync(int id);
    Task<IEnumerable<Invoice>> GetByEmployeeIdAsync(int employeeId);
    Task<IEnumerable<Invoice>> GetAllAsync();
    Task<bool> ExistsOverlappingInvoiceAsync(int employeeId, DateTime startDate, DateTime endDate, int? excludeInvoiceId = null);
    Task AddAsync(Invoice invoice);
    Task UpdateAsync(Invoice invoice);
}

// InvoiceSystem.Domain/Interfaces/IUnitOfWork.cs
public interface IUnitOfWork
{
    IEmployeeRepository Employees { get; }
    IContractRepository Contracts { get; }
    IInvoiceRepository Invoices { get; }
    Task<int> SaveChangesAsync();
}
```

## Application Layer Implementation

### DTOs

```csharp
// InvoiceSystem.Application/DTOs/EmployeeDto.cs
public class EmployeeDto
{
    public int Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string EmployeeIdentifier { get; set; }
    public string FullName => $"{FirstName} {LastName}";
}

// InvoiceSystem.Application/DTOs/ContractDto.cs
public class ContractDto
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal DailyRate { get; set; }
}

// InvoiceSystem.Application/DTOs/InvoiceDto.cs
public class InvoiceDto
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
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
    public int EmployeeId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int DaysWorked { get; set; }
}
```

### CQRS Implementation

```csharp
// InvoiceSystem.Application/Commands/CreateInvoiceCommand.cs
public class CreateInvoiceCommand : IRequest<Result<int>>
{
    public int EmployeeId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int DaysWorked { get; set; }
}

// InvoiceSystem.Application/Commands/CreateInvoiceCommandHandler.cs
public class CreateInvoiceCommandHandler : IRequestHandler<CreateInvoiceCommand, Result<int>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IInvoiceValidationService _validationService;
    
    public CreateInvoiceCommandHandler(
        IUnitOfWork unitOfWork,
        IInvoiceValidationService validationService)
    {
        _unitOfWork = unitOfWork;
        _validationService = validationService;
    }
    
    public async Task<Result<int>> Handle(CreateInvoiceCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Get employee
            var employee = await _unitOfWork.Employees.GetByIdAsync(request.EmployeeId);
            if (employee == null)
                return Result<int>.Failure("Employee not found");
                
            // Create date range
            var dateRange = new DateRange(request.StartDate, request.EndDate);
            
            // Get contract to calculate expected amount
            var contract = employee.GetValidContractForPeriod(dateRange);
            if (contract == null)
                return Result<int>.Failure("No valid contract exists for this invoice period");
                
            decimal expectedAmount = contract.DailyRate.Amount * request.DaysWorked;
            
            // Validate invoice
            try
            {
                await _validationService.ValidateInvoiceAsync(
                    employee, 
                    dateRange, 
                    request.DaysWorked, 
                    expectedAmount);
            }
            catch (DomainException ex)
            {
                return Result<int>.Failure(ex.Message);
            }
            
            // Create invoice
            var invoice = Invoice.Create(
                employee,
                dateRange,
                request.DaysWorked);
                
            // Save invoice
            await _unitOfWork.Invoices.AddAsync(invoice);
            await _unitOfWork.SaveChangesAsync();
            
            return Result<int>.Success(invoice.Id);
        }
        catch (Exception ex)
        {
            return Result<int>.Failure($"An error occurred: {ex.Message}");
        }
    }
}

// InvoiceSystem.Application/Queries/GetInvoicesQuery.cs
public class GetInvoicesQuery : IRequest<Result<IEnumerable<InvoiceDto>>>
{
    public int? EmployeeId { get; set; }
}

// InvoiceSystem.Application/Queries/GetInvoicesQueryHandler.cs
public class GetInvoicesQueryHandler : IRequestHandler<GetInvoicesQuery, Result<IEnumerable<InvoiceDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    
    public GetInvoicesQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
    
    public async Task<Result<IEnumerable<InvoiceDto>>> Handle(GetInvoicesQuery request, CancellationToken cancellationToken)
    {
        try
        {
            IEnumerable<Invoice> invoices;
            
            if (request.EmployeeId.HasValue)
                invoices = await _unitOfWork.Invoices.GetByEmployeeIdAsync(request.EmployeeId.Value);
            else
                invoices = await _unitOfWork.Invoices.GetAllAsync();
                
            var invoiceDtos = invoices.Select(i => new InvoiceDto
            {
                Id = i.Id,
                EmployeeId = i.EmployeeId,
                EmployeeName = i.Employee?.FullName ?? "Unknown",
                StartDate = i.DateRange.Start,
                EndDate = i.DateRange.End,
                DaysWorked = i.DaysWorked,
                TotalAmount = i.TotalAmount.Amount,
                CreatedAt = i.CreatedAt
            });
            
            return Result<IEnumerable<InvoiceDto>>.Success(invoiceDtos);
        }
        catch (Exception ex)
        {
            return Result<IEnumerable<InvoiceDto>>.Failure($"An error occurred: {ex.Message}");
        }
    }
}
```

### Result Class

```csharp
// InvoiceSystem.Application/Common/Result.cs
public class Result<T>
{
    public bool IsSuccess { get; }
    public T Value { get; }
    public string Error { get; }
    
    private Result(bool isSuccess, T value, string error)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
    }
    
    public static Result<T> Success(T value) => new Result<T>(true, value, null);
    public static Result<T> Failure(string error) => new Result<T>(false, default, error);
}

public class Result
{
    public bool IsSuccess { get; }
    public string Error { get; }
    
    private Result(bool isSuccess, string error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }
    
    public static Result Success() => new Result(true, null);
    public static Result Failure(string error) => new Result(false, error);
}
```

## Infrastructure Layer Implementation

### Data Context

```csharp
// InvoiceSystem.Infrastructure/Data/InvoiceSystemDbContext.cs
public class InvoiceSystemDbContext : DbContext
{
    public DbSet<Employee> Employees { get; set; }
    public DbSet<Contract> Contracts { get; set; }
    public DbSet<Invoice> Invoices { get; set; }
    public DbSet<User> Users { get; set; }
    
    public InvoiceSystemDbContext(DbContextOptions<InvoiceSystemDbContext> options)
        : base(options)
    {
    }
    
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
            
            entity.HasMany<Contract>()
                  .WithOne()
                  .HasForeignKey(c => c.EmployeeId)
                  .OnDelete(DeleteBehavior.Cascade);
                  
            entity.HasMany<Invoice>()
                  .WithOne()
                  .HasForeignKey(i => i.EmployeeId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
        
        // Contract configuration
        modelBuilder.Entity<Contract>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.EmployeeId).IsRequired();
            
            entity.OwnsOne(c => c.DateRange, dr =>
            {
                dr.Property(d => d.Start).HasColumnName("StartDate").IsRequired();
                dr.Property(d => d.End).HasColumnName("EndDate").IsRequired();
            });
            
            entity.OwnsOne(c => c.DailyRate, dr =>
            {
                dr.Property(d => d.Amount).HasColumnName("DailyRate").IsRequired().HasColumnType("decimal(18,2)");
                dr.Property(d => d.Currency).HasColumnName("Currency").IsRequired().HasMaxLength(3);
            });
        });
        
        // Invoice configuration
        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.HasKey(i => i.Id);
            entity.Property(i => i.EmployeeId).IsRequired();
            entity.Property(i => i.DaysWorked).IsRequired();
            entity.Property(i => i.CreatedAt).IsRequired();
            
            entity.OwnsOne(i => i.DateRange, dr =>
            {
                dr.Property(d => d.Start).HasColumnName("StartDate").IsRequired();
                dr.Property(d => d.End).HasColumnName("EndDate").IsRequired();
            });
            
            entity.OwnsOne(i => i.TotalAmount, ta =>
            {
                ta.Property(t => t.Amount).HasColumnName("TotalAmount").IsRequired().HasColumnType("decimal(18,2)");
                ta.Property(t => t.Currency).HasColumnName("Currency").IsRequired().HasMaxLength(3);
            });
        });
        
        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.Property(u => u.Username).IsRequired().HasMaxLength(50);
            entity.Property(u => u.PasswordHash).IsRequired();
            entity.Property(u => u.IsInvoiceManager).IsRequired();
            entity.HasIndex(u => u.Username).IsUnique();
        });