using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using InvoiceSystem.Domain.DTOs;
using InvoiceSystem.Domain.Interfaces;
using InvoiceSystem.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;

namespace InvoiceSystem.Web.Controllers
{
    [Authorize(Roles = "InvoiceManager")]
    public class ContractController : Controller
    {
        private readonly IEmployeeService _employeeService;
        private readonly ILogger<ContractController> _logger;

        public ContractController(
            IEmployeeService employeeService,
            ILogger<ContractController> logger)
        {
            _employeeService = employeeService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var contracts = await _employeeService.GetAllContractsAsync();
            return View(contracts);
        }

        public async Task<IActionResult> Create()
        {
            var employees = await _employeeService.GetAllEmployeesAsync();
            ViewBag.Employees = new SelectList(employees, "Id", "FullName");
            ViewBag.ContractTypes = new SelectList(Enum.GetValues(typeof(ContractType)));
            ViewBag.PayGrades = new SelectList(Enum.GetValues(typeof(PayGrade)));
            
            return View(new CreateContractDto());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateContractDto dto)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    await _employeeService.CreateContractAsync(dto);
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating contract");
                ModelState.AddModelError("", ex.Message);
            }

            var employees = await _employeeService.GetAllEmployeesAsync();
            ViewBag.Employees = new SelectList(employees, "Id", "FullName");
            ViewBag.ContractTypes = new SelectList(Enum.GetValues(typeof(ContractType)));
            ViewBag.PayGrades = new SelectList(Enum.GetValues(typeof(PayGrade)));
            
            return View(dto);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var contract = await _employeeService.GetContractByIdAsync(id);
            if (contract == null)
                return NotFound();

            var employees = await _employeeService.GetAllEmployeesAsync();
            ViewBag.Employees = new SelectList(employees, "Id", "FullName");
            ViewBag.ContractTypes = new SelectList(Enum.GetValues(typeof(ContractType)));
            ViewBag.PayGrades = new SelectList(Enum.GetValues(typeof(PayGrade)));

            return View(contract);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(UpdateContractDto dto)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    await _employeeService.UpdateContractAsync(dto);
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating contract");
                ModelState.AddModelError("", ex.Message);
            }

            var employees = await _employeeService.GetAllEmployeesAsync();
            ViewBag.Employees = new SelectList(employees, "Id", "FullName");
            ViewBag.ContractTypes = new SelectList(Enum.GetValues(typeof(ContractType)));
            ViewBag.PayGrades = new SelectList(Enum.GetValues(typeof(PayGrade)));

            return View(dto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _employeeService.DeleteContractAsync(id);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting contract");
                return RedirectToAction(nameof(Index));
            }
        }
    }
} 