using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;
using TestMVC.Interface;
using TestMVC.Models;

namespace TestMVC.Controllers.Api
{
    [ApiController]
    [Route("api/Sales")]
    public class SalesApiController : ControllerBase
    {
        private readonly ISalesService _salesService;

        public SalesApiController(ISalesService salesService)
        {
            _salesService = salesService;
        }

        [HttpGet]
        [Route("sales_invoice_list_get")]
        public async Task<IActionResult> Search([FromQuery] string? search)
        {
            int? currentUserId = Utility.Helper.GetUserId(User);
            var invoices = await _salesService.GetInvoices(search, currentUserId);
            var todaySales = await _salesService.GetTodaySalesAmount(currentUserId);

            return Ok(new
            {
                success = true,
                data = invoices.Select(inv => new
                {
                    salesInvoiceId = inv.SalesInvoiceId,
                    invoiceNumber = inv.InvoiceNumber,
                    customerId = inv.CustomerId,
                    customerName = inv.CustomerName,
                    customerPhone = inv.CustomerPhone,
                    invoiceDate = inv.InvoiceDate.ToString("yyyy-MM-dd hh:mm tt"),
                    grandTotal = inv.GrandTotal,
                    grandTotalFormatted = inv.GrandTotal.ToString("N2"),
                    paidAmount = inv.PaidAmount,
                    paidAmountFormatted = inv.PaidAmount.ToString("N2"),
                    dueAmount = inv.DueAmount,
                    dueAmountFormatted = inv.DueAmount.ToString("N2"),
                    status = inv.Status
                }),
                todaySales,
                todaySalesFormatted = todaySales.ToString("N2"),
                totalInvoices = invoices.Count,
                totalRecords = invoices.Count
            });
        }

        [HttpGet]
        [Route("sales_invoice_detail_get")]
        public async Task<IActionResult> GetById([FromQuery(Name = "id")] long? queryId = null, long? id = null)
        {
            long targetId = (queryId ?? 0) > 0 ? queryId!.Value : (id ?? 0);
            if (targetId <= 0) return BadRequest(new { success = false, message = "Invalid invoice ID." });

            int? currentUserId = Utility.Helper.GetUserId(User);
            var invoice = await _salesService.GetInvoiceById(targetId, currentUserId);
            if (invoice == null)
            {
                return NotFound(new { success = false, message = "Invoice not found." });
            }

            return Ok(new { success = true, data = invoice });
        }

        [HttpPost]
        [Route("sales_invoice_create")]
        public async Task<IActionResult> Create([FromBody] SalesInvoice invoice)
        {
            if (invoice == null)
            {
                return BadRequest(new { success = false, message = "Request body is required." });
            }

            if (invoice.Items == null || invoice.Items.Count == 0)
            {
                ModelState.AddModelError("Items", "Please add at least one product to the invoice bill.");
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState.ToDictionary(
                    k => k.Key,
                    v => v.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>()
                );
                return BadRequest(new { success = false, message = "Validation failed.", errors });
            }

            try
            {
                int? currentUserId = Utility.Helper.GetUserId(User);
                invoice.UserId = currentUserId;
                invoice.CreatedBy = currentUserId;

                long id = await _salesService.CreateSalesInvoice(invoice);
                if (id > 0)
                {
                    return StatusCode(201, new
                    {
                        success = true,
                        id,
                        invoiceNumber = invoice.InvoiceNumber,
                        message = $"Invoice #{invoice.InvoiceNumber} generated successfully!",
                        redirectUrl = $"/Sales/Invoice/{id}"
                    });
                }

                return BadRequest(new { success = false, message = "Unable to create sales invoice." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = $"Error generating invoice: {ex.Message}" });
            }
        }
    }
}
