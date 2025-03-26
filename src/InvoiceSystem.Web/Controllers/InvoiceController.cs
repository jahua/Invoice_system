using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using InvoiceSystem.Domain.DTOs;
using InvoiceSystem.Domain.Interfaces;
using InvoiceSystem.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authentication;

namespace InvoiceSystem.Web.Controllers
{
    [Authorize]
    public class InvoiceController : Controller
    {
        private readonly IInvoiceService _invoiceService;
        private readonly IEmployeeService _employeeService;
        private readonly IAuthService _authService;
        private readonly ILogger<InvoiceController> _logger;

        public InvoiceController(
            IInvoiceService invoiceService, 
            IEmployeeService employeeService, 
            IAuthService authService,
            ILogger<InvoiceController> logger)
        {
            _invoiceService = invoiceService;
            _employeeService = employeeService;
            _authService = authService;
            _logger = logger;
        }

        [Authorize(Roles = "InvoiceManager")]
        public async Task<IActionResult> Index()
        {
            try
            {
                var user = await _authService.GetCurrentUserAsync();
                if (user == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                if (!await _authService.IsInvoiceManagerAsync(user.Id))
                {
                    return RedirectToAction(nameof(MyInvoices));
                }

                _logger.LogInformation("Retrieving all invoices for manager view");
                var invoices = await _invoiceService.GetAllInvoicesAsync();
                _logger.LogInformation("Retrieved {Count} invoices", invoices?.Count() ?? 0);
                return View(invoices);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving invoices");
                TempData["ErrorMessage"] = "An error occurred while retrieving invoices.";
                return View(new List<InvoiceDto>());
            }
        }

        public async Task<IActionResult> MyInvoices()
        {
            try
            {
                var user = await _authService.GetCurrentUserAsync();
                if (user?.EmployeeId == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                var invoices = await _invoiceService.GetEmployeeInvoicesAsync(user.EmployeeId.Value);
                return View(invoices);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving employee invoices");
                TempData["ErrorMessage"] = "An error occurred while retrieving your invoices.";
                return View(new List<InvoiceDto>());
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var currentUser = await _authService.GetCurrentUserAsync();
                if (currentUser == null)
                    return RedirectToAction("Login", "Account");

                var invoice = await _invoiceService.GetInvoiceByIdAsync(id);
                if (invoice == null)
                    return NotFound();

                // Only allow deletion if user is a manager or if it's their own invoice
                if (!currentUser.IsManager && (!currentUser.EmployeeId.HasValue || invoice.Employee.Id != currentUser.EmployeeId.Value))
                    return Forbid();

                await _invoiceService.DeleteInvoiceAsync(id);
                
                // Redirect back to the appropriate page based on where the delete was initiated
                if (Request.Headers["Referer"].ToString().Contains("MyInvoices"))
                    return RedirectToAction(nameof(MyInvoices));
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting invoice {InvoiceId}", id);
                return RedirectToAction(nameof(Index));
            }
        }

        public async Task<IActionResult> Details(int id)
        {
            var invoice = await _invoiceService.GetInvoiceByIdAsync(id);
            if (invoice == null)
                return NotFound();

            var currentUser = await _authService.GetCurrentUserAsync();
            if (currentUser == null)
                return RedirectToAction("Login", "Account");

            if (!currentUser.IsManager && (!currentUser.EmployeeId.HasValue || invoice.Employee.Id != currentUser.EmployeeId.Value))
                return Forbid();

            return View(invoice);
        }

        [HttpGet]
        public async Task<IActionResult> Create(string? source)
        {
            try
            {
                var user = await _authService.GetCurrentUserAsync();
                if (user?.EmployeeId == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                // If not a manager and source is not MyInvoices or Employee, redirect to MyInvoices
                if (!await _authService.IsInvoiceManagerAsync(user.Id) && 
                    source != "MyInvoices" && source != "Employee")
                {
                    return RedirectToAction(nameof(MyInvoices));
                }

                // Get the employee's own data
                var employee = await _employeeService.GetEmployeeByIdAsync(user.EmployeeId.Value);
                if (employee == null)
                {
                    return NotFound("Employee not found");
                }

                // Get the employee's contracts
                var contracts = await _employeeService.GetEmployeeContractsAsync(user.EmployeeId.Value);
                if (!contracts.Any())
                {
                    TempData["ErrorMessage"] = "No active contracts found. Please contact your manager.";
                    if (source == "Employee")
                    {
                        return RedirectToAction("Employee", "Home");
                    }
                    return RedirectToAction(source == "MyInvoices" ? nameof(MyInvoices) : nameof(Index));
                }

                ViewBag.Contracts = new SelectList(contracts, "Id", "ContractDisplayName");
                ViewBag.Employees = new SelectList(new[] { employee }, "Id", "FullName");
                ViewBag.IsEmployee = true;
                ViewBag.EmployeeId = employee.Id;
                ViewBag.Source = source;

                var model = new CreateInvoiceDto
                {
                    StartDate = DateTime.Today,
                    EndDate = DateTime.Today,
                    EmployeeId = employee.Id
                };

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error preparing create invoice view");
                TempData["ErrorMessage"] = "An error occurred while preparing the invoice form.";
                if (source == "Employee")
                {
                    return RedirectToAction("Employee", "Home");
                }
                return RedirectToAction(source == "MyInvoices" ? nameof(MyInvoices) : nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateInvoiceDto model, string? source)
        {
            try
            {
                var user = await _authService.GetCurrentUserAsync();
                if (user?.EmployeeId == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                // Always ensure the EmployeeId matches the current user when creating from MyInvoices or Employee
                if (source == "MyInvoices" || source == "Employee")
                {
                    model.EmployeeId = user.EmployeeId.Value;
                }
                else if (!await _authService.IsInvoiceManagerAsync(user.Id))
                {
                    // If not a manager and source is not MyInvoices or Employee, redirect to MyInvoices
                    return RedirectToAction(nameof(MyInvoices));
                }

                if (!ModelState.IsValid)
                {
                    await PrepareCreateView(user);
                    ViewBag.Source = source;
                    return View(model);
                }

                var invoice = await _invoiceService.CreateInvoiceAsync(model);
                TempData["SuccessMessage"] = "Invoice created successfully";
                
                // Handle redirection based on source and user role
                if (source == "Employee")
                {
                    return RedirectToAction("Employee", "Home");
                }
                if (source == "MyInvoices" || !await _authService.IsInvoiceManagerAsync(user.Id))
                {
                    return RedirectToAction(nameof(MyInvoices));
                }
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating invoice: {Message}", ex.Message);
                ModelState.AddModelError("", "Error creating invoice: " + ex.Message);
                await PrepareCreateView(await _authService.GetCurrentUserAsync());
                ViewBag.Source = source;
                return View(model);
            }
        }

        private async Task PrepareCreateView(User user)
        {
            if (!await _authService.IsInvoiceManagerAsync(user.Id))
            {
                var employee = await _employeeService.GetEmployeeByIdAsync(user.EmployeeId.Value);
                var contracts = await _employeeService.GetEmployeeContractsAsync(user.EmployeeId.Value);
                ViewBag.Contracts = new SelectList(contracts, "Id", "ContractDisplayName");
                ViewBag.Employees = new SelectList(new[] { employee }, "Id", "FullName");
                ViewBag.IsEmployee = true;
                ViewBag.EmployeeId = employee.Id;
            }
            else
            {
                var employees = await _employeeService.GetAllEmployeesAsync();
                ViewBag.Employees = new SelectList(employees, "Id", "FullName");
                ViewBag.IsEmployee = false;
            }
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var invoice = await _invoiceService.GetInvoiceByIdAsync(id);
            if (invoice == null)
            {
                return NotFound();
            }

            var updateDto = new UpdateInvoiceDto
            {
                Id = invoice.Id,
                EmployeeId = invoice.Employee.Id,
                EmployeeName = $"{invoice.Employee.FirstName} {invoice.Employee.LastName}",
                ContractId = invoice.ContractId,
                ContractType = invoice.ContractType.ToString(),
                PayGrade = invoice.PayGrade.ToString(),
                DailyRate = invoice.Contract.DailyRate,
                StartDate = invoice.StartDate,
                EndDate = invoice.EndDate,
                DaysWorked = invoice.DaysWorked,
                TotalAmount = invoice.TotalAmount,
                Status = invoice.Status
            };

            return View(updateDto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(UpdateInvoiceDto updateDto)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    await _invoiceService.UpdateInvoiceAsync(updateDto);
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", ex.Message);
                }
            }
            return View(updateDto);
        }

        [HttpGet]
        public async Task<IActionResult> GetContractDetails(int id)
        {
            try
            {
                var contract = await _employeeService.GetContractByIdAsync(id);
                if (contract == null)
                {
                    return NotFound();
                }

                return Json(new
                {
                    id = contract.Id,
                    startDate = contract.StartDate.ToString("yyyy-MM-dd"),
                    endDate = contract.EndDate.ToString("yyyy-MM-dd"),
                    dailyRate = contract.DailyRate,
                    payGrade = contract.PayGrade,
                    contractType = contract.ContractType
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting contract details");
                return StatusCode(500, "Error retrieving contract details");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetEmployeeContracts(int employeeId)
        {
            try
            {
                var contracts = await _employeeService.GetEmployeeContractsAsync(employeeId);
                if (!contracts.Any())
                {
                    return NotFound("No contracts found for this employee");
                }
                return Json(contracts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting employee contracts");
                return StatusCode(500, "Error retrieving employee contracts");
            }
        }
    }
} 