using Microsoft.AspNetCore.Mvc;
using InvoiceSystem.Domain.Interfaces;
using InvoiceSystem.Domain.DTOs;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace InvoiceSystem.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly IInvoiceService _invoiceService;
        private readonly IEmployeeService _employeeService;
        private readonly IAuthService _authService;
        private readonly ILogger<HomeController> _logger;

        public HomeController(
            IInvoiceService invoiceService, 
            IEmployeeService employeeService,
            IAuthService authService,
            ILogger<HomeController> logger)
        {
            _invoiceService = invoiceService;
            _employeeService = employeeService;
            _authService = authService;
            _logger = logger;
        }

        public IActionResult Index()
        {
            if (!User.Identity?.IsAuthenticated ?? true)
            {
                return RedirectToAction("Login", "Account");
            }

            if (User.IsInRole("InvoiceManager"))
            {
                return RedirectToAction("Index", "Invoice");
            }

            return RedirectToAction("Employee");
        }

        [Authorize]
        public async Task<IActionResult> Employee()
        {
            try
            {
                var user = await _authService.GetCurrentUserAsync();
                if (user == null || !user.EmployeeId.HasValue)
                {
                    _logger.LogWarning("User without EmployeeId attempted to access Employee view");
                    return RedirectToAction("Login", "Account");
                }

                _logger.LogInformation("Retrieving data for employee {EmployeeId}", user.EmployeeId.Value);

                // Get personal invoices
                var personalInvoices = await _invoiceService.GetInvoicesByEmployeeIdAsync(user.EmployeeId.Value);
                _logger.LogInformation("Retrieved {Count} invoices for employee", personalInvoices?.Count() ?? 0);

                // Get personal contracts
                var personalContracts = await _employeeService.GetEmployeeContractsAsync(user.EmployeeId.Value);
                _logger.LogInformation("Retrieved {Count} contracts for employee", personalContracts?.Count() ?? 0);
                
                // Initialize empty collections if null
                if (personalInvoices == null)
                {
                    _logger.LogWarning("No invoices found for employee {EmployeeId}", user.EmployeeId.Value);
                    personalInvoices = new List<InvoiceDto>();
                }

                if (personalContracts == null)
                {
                    _logger.LogWarning("No contracts found for employee {EmployeeId}", user.EmployeeId.Value);
                    personalContracts = new List<ContractDto>();
                }

                ViewBag.PersonalContracts = personalContracts;
                ViewBag.PersonalInvoices = personalInvoices;
                ViewBag.Source = "Employee";
                ViewBag.EmployeeId = user.EmployeeId.Value;

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Employee action: {Message}", ex.Message);
                TempData["ErrorMessage"] = "An error occurred while retrieving your data.";
                ViewBag.PersonalContracts = new List<ContractDto>();
                ViewBag.PersonalInvoices = new List<InvoiceDto>();
                ViewBag.Source = "Employee";
                return View();
            }
        }

        [Authorize(Roles = "InvoiceManager")]
        public async Task<IActionResult> InvoiceManager()
        {
            try
            {
                var user = await _authService.GetCurrentUserAsync();
                if (user == null)
                {
                    _logger.LogWarning("No user found in GetCurrentUserAsync");
                    return RedirectToAction("Login", "Account");
                }

                var allInvoices = await _invoiceService.GetAllInvoicesAsync();
                return View(allInvoices);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in InvoiceManager action");
                TempData["ErrorMessage"] = "An error occurred while retrieving the data.";
                return View(new List<InvoiceDto>());
            }
        }

        [Authorize]
        public async Task<IActionResult> MyInvoices()
        {
            try
            {
                var user = await _authService.GetCurrentUserAsync();
                if (user == null || !user.EmployeeId.HasValue)
                {
                    _logger.LogWarning("User without EmployeeId attempted to access MyInvoices");
                    return RedirectToAction("Login", "Account");
                }

                var invoices = await _invoiceService.GetInvoicesByEmployeeIdAsync(user.EmployeeId.Value);
                var contracts = await _employeeService.GetEmployeeContractsAsync(user.EmployeeId.Value);
                
                ViewBag.Contracts = contracts;
                return View(invoices);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving invoices for employee");
                TempData["ErrorMessage"] = "An error occurred while retrieving your invoices.";
                return View(new List<InvoiceDto>());
            }
        }

        [Authorize(Roles = "InvoiceManager")]
        public async Task<IActionResult> AllInvoices()
        {
            try
            {
                var invoices = await _invoiceService.GetAllInvoicesAsync();
                return View(invoices);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all invoices");
                TempData["ErrorMessage"] = "An error occurred while retrieving all invoices.";
                return View(new List<InvoiceDto>());
            }
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View();
        }
    }
} 