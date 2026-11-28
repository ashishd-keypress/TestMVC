using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;
using TestMVC.Interface;
using TestMVC.Models;

namespace TestMVC.Controllers.Api
{
    [ApiController]
    [Route("api/Purchase")]
    public class PurchaseApiController : ControllerBase
    {
        private readonly IPurchaseService _purchaseService;

        public PurchaseApiController(IPurchaseService purchaseService)
        {
            _purchaseService = purchaseService;
        }

        [HttpGet]
        [Route("purchase_list_get")]
        public async Task<IActionResult> Search([FromQuery] string? search)
        {
            int? currentUserId = Utility.Helper.GetUserId(User);
            var purchases = await _purchaseService.GetPurchases(search, currentUserId);

            return Ok(new
            {
                success = true,
                data = purchases.Select(p => new
                {
                    purchaseId = p.PurchaseId,
                    supplierId = p.SupplierId,
                    supplierName = p.SupplierName,
                    supplierPhone = p.SupplierPhone,
                    invoiceNumber = p.InvoiceNumber,
                    purchaseDate = p.PurchaseDate.ToString("yyyy-MM-dd"),
                    subTotal = p.SubTotal,
                    discount = p.Discount,
                    taxAmount = p.TaxAmount,
                    grandTotal = p.GrandTotal,
                    grandTotalFormatted = p.GrandTotal.ToString("N2"),
                    paidAmount = p.PaidAmount,
                    paidAmountFormatted = p.PaidAmount.ToString("N2"),
                    dueAmount = p.DueAmount,
                    dueAmountFormatted = p.DueAmount.ToString("N2"),
                    status = p.Status
                }),
                totalRecords = purchases.Count
            });
        }

        [HttpGet]
        [Route("purchase_detail_get")]
        public async Task<IActionResult> GetById([FromQuery(Name = "id")] long? queryId = null, long? id = null)
        {
            long targetId = (queryId ?? 0) > 0 ? queryId!.Value : (id ?? 0);
            if (targetId <= 0) return BadRequest(new { success = false, message = "Invalid purchase ID." });

            int? currentUserId = Utility.Helper.GetUserId(User);
            var purchase = await _purchaseService.GetPurchaseById(targetId, currentUserId);
            if (purchase == null)
            {
                return NotFound(new { success = false, message = "Purchase record not found." });
            }

            return Ok(new { success = true, data = purchase });
        }

        [HttpPost]
        [Route("purchase_detail_create")]
        public async Task<IActionResult> Create([FromBody] Purchase purchase)
        {
            if (purchase == null)
            {
                return BadRequest(new { success = false, message = "Request body is required." });
            }

            if (purchase.Items == null || purchase.Items.Count == 0)
            {
                ModelState.AddModelError("Items", "Please add at least one product item to the purchase.");
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
                purchase.UserId = currentUserId;
                purchase.CreatedBy = currentUserId;

                long id = await _purchaseService.SavePurchase(purchase);
                if (id > 0)
                {
                    return StatusCode(201, new
                    {
                        success = true,
                        id,
                        invoiceNumber = purchase.InvoiceNumber,
                        message = $"Purchase invoice #{purchase.InvoiceNumber} saved and inventory updated successfully!",
                        redirectUrl = $"/Purchase/Details/{id}"
                    });
                }

                return BadRequest(new { success = false, message = "Unable to save purchase." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = $"Error saving purchase: {ex.Message}" });
            }
        }
    }
}
