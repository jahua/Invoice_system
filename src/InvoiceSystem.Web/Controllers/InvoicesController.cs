using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using InvoiceSystem.Domain.DTOs;
using InvoiceSystem.Domain.Interfaces;
using InvoiceSystem.Domain.Enums;

namespace InvoiceSystem.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Require authentication for all endpoints
    public class InvoicesController : ControllerBase
    {
        private readonly IInvoiceService _invoiceService;
        private readonly IAuthService _authService;

        public InvoicesController(IInvoiceService invoiceService, IAuthService authService)
        {
            _invoiceService = invoiceService;
            _authService = authService;
        }

        [HttpGet]
        [Authorize(Roles = "InvoiceManager")] // Only managers can see all invoices
        public async Task<ActionResult<List<InvoiceDto>>> GetAll()
        {
            var invoices = await _invoiceService.GetAllInvoicesAsync();
            return Ok(invoices);
        }

        [HttpGet("my-invoices")]
        public async Task<ActionResult<List<InvoiceDto>>> GetMyInvoices()
        {
            var user = await _authService.GetCurrentUserAsync();
            if (user?.EmployeeId == null)
            {
                return Forbid();
            }

            var invoices = await _invoiceService.GetEmployeeInvoicesAsync(user.EmployeeId.Value);
            return Ok(invoices);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<InvoiceDto>> GetById(int id)
        {
            var user = await _authService.GetCurrentUserAsync();
            var invoice = await _invoiceService.GetInvoiceByIdAsync(id);
            
            if (invoice == null)
                return NotFound();

            // Check if user has access to this invoice
            if (!await _authService.IsInvoiceManagerAsync(user.Id) && 
                user?.EmployeeId != invoice.EmployeeId)
            {
                return Forbid();
            }

            return Ok(invoice);
        }

        [HttpPost]
        public async Task<ActionResult<InvoiceDto>> Create([FromBody] CreateInvoiceDto model)
        {
            try
            {
                var user = await _authService.GetCurrentUserAsync();
                if (user?.EmployeeId == null)
                {
                    return Forbid();
                }

                // Ensure the employee can only create invoices for themselves
                if (model.EmployeeId != user.EmployeeId)
                {
                    return BadRequest("You can only create invoices for yourself");
                }

                var invoice = await _invoiceService.CreateInvoiceAsync(model);
                return CreatedAtAction(nameof(GetById), new { id = invoice.Id }, invoice);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<InvoiceDto>> Update(int id, [FromBody] UpdateInvoiceDto model)
        {
            try
            {
                var user = await _authService.GetCurrentUserAsync();
                var existingInvoice = await _invoiceService.GetInvoiceByIdAsync(id);

                if (existingInvoice == null)
                    return NotFound();

                // Check if user has access to update this invoice
                if (!await _authService.IsInvoiceManagerAsync(user.Id) && 
                    user?.EmployeeId != existingInvoice.EmployeeId)
                {
                    return Forbid();
                }

                // Only allow updates if invoice is in Draft or Rejected status
                if (existingInvoice.Status != InvoiceStatus.Draft && 
                    existingInvoice.Status != InvoiceStatus.Rejected)
                {
                    return BadRequest("Can only edit invoices in Draft or Rejected status");
                }

                model.Id = id;
                var updatedInvoice = await _invoiceService.UpdateInvoiceAsync(model);
                return Ok(updatedInvoice);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            var user = await _authService.GetCurrentUserAsync();
            var invoice = await _invoiceService.GetInvoiceByIdAsync(id);

            if (invoice == null)
                return NotFound();

            // Check if user has access to delete this invoice
            if (!await _authService.IsInvoiceManagerAsync(user.Id) && 
                user?.EmployeeId != invoice.EmployeeId)
            {
                return Forbid();
            }

            // Only allow deletion if invoice is in Draft status
            if (invoice.Status != InvoiceStatus.Draft)
            {
                return BadRequest("Can only delete invoices in Draft status");
            }

            var result = await _invoiceService.DeleteInvoiceAsync(id);
            if (!result)
                return NotFound();

            return NoContent();
        }
    }
} 