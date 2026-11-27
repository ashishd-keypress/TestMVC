using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TestMVC.Interface;
using TestMVC.Models;
using TestMVC.Models.ReqRes;

namespace TestMVC.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    public class SupplierController : ControllerBase
    {
        private readonly ISupplierService _supplierService;

        public SupplierController(ISupplierService supplierService)
        {
            _supplierService = supplierService;
        }

        [HttpGet]
        [Route("")]
        [Route("supplier_list_get")]
        [Route("search")]
        public async Task<IActionResult> Search([FromQuery] string? search)
        {
            int? currentUserId = Utility.Helper.GetUserId(User);
            var suppliers = await _supplierService.GetSuppliers(search, currentUserId);

            return Ok(new
            {
                success = true,
                data = suppliers.Select(s => new
                {
                    supplierId = s.SupplierId,
                    supplierName = s.SupplierName,
                    contactPerson = s.ContactPerson,
                    phone = s.Phone,
                    email = s.Email,
                    gstNumber = s.GSTNumber,
                    address = s.Address,
                    openingBalance = s.OpeningBalance,
                    currentBalance = s.CurrentBalance,
                    currentBalanceFormatted = s.CurrentBalance.ToString("N2"),
                    isActive = s.IsActive,
                    userId = s.UserId
                }),
                totalRecords = suppliers.Count
            });
        }

        [HttpGet]
        [Route("supplier_detail_get")]
        [Route("{id:long}")]
        public async Task<IActionResult> GetById([FromQuery(Name = "id")] long? queryId = null, long? id = null)
        {
            long targetId = (queryId ?? 0) > 0 ? queryId!.Value : (id ?? 0);
            if (targetId <= 0) return BadRequest(new { success = false, message = "Invalid supplier identifier." });

            int? currentUserId = Utility.Helper.GetUserId(User);
            var supplier = await _supplierService.GetSupplierById(targetId, currentUserId);
            if (supplier == null)
            {
                return NotFound(new { success = false, message = "Supplier not found." });
            }

            return Ok(new { success = true, data = supplier });
        }

        [HttpPost]
        [Route("")]
        [Route("supplier_detail_create")]
        [Route("create")]
        public async Task<IActionResult> Create([FromBody] Supplier supplier)
        {
            if (supplier == null) return BadRequest(new { success = false, message = "Request body is required." });

            if (!string.IsNullOrWhiteSpace(supplier.Phone))
            {
                supplier.Phone = Regex.Replace(supplier.Phone, @"[^0-9]", "");
            }

            if (string.IsNullOrWhiteSpace(supplier.Phone) || !Regex.IsMatch(supplier.Phone, @"^[0-9]{10,15}$"))
            {
                ModelState.AddModelError(nameof(supplier.Phone), "Phone number must contain only digits (10 to 15 digits).");
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState.ToDictionary(
                    k => k.Key,
                    v => v.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>()
                );
                return BadRequest(new { success = false, message = "Validation failed.", errors });
            }

            int? currentUserId = Utility.Helper.GetUserId(User);
            supplier.SupplierId = 0;
            supplier.UserId = currentUserId;

            var success = await _supplierService.SaveSupplier(supplier);
            if (!success)
            {
                return BadRequest(new { success = false, message = "Unable to save supplier. Please check details." });
            }

            return StatusCode(201, new
            {
                success = true,
                message = "Supplier added successfully.",
                data = supplier,
                redirectUrl = "/Supplier"
            });
        }

        [HttpPut]
        [HttpPost]
        [Route("supplier_detail_update")]
        [Route("{id:long}")]
        [Route("update/{id:long}")]
        [Route("update")]
        public async Task<IActionResult> Update([FromBody] Supplier supplier, [FromQuery(Name = "id")] long? queryId = null, long? id = null)
        {
            if (supplier == null) return BadRequest(new { success = false, message = "Request body is required." });

            if ((queryId ?? 0) > 0)
            {
                supplier.SupplierId = queryId!.Value;
            }
            else if (id.HasValue && id.Value > 0)
            {
                supplier.SupplierId = id.Value;
            }

            if (supplier.SupplierId <= 0)
            {
                return BadRequest(new { success = false, message = "Valid SupplierId is required." });
            }

            if (!string.IsNullOrWhiteSpace(supplier.Phone))
            {
                supplier.Phone = Regex.Replace(supplier.Phone, @"[^0-9]", "");
            }

            if (string.IsNullOrWhiteSpace(supplier.Phone) || !Regex.IsMatch(supplier.Phone, @"^[0-9]{10,15}$"))
            {
                ModelState.AddModelError(nameof(supplier.Phone), "Phone number must contain only digits (10 to 15 digits).");
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState.ToDictionary(
                    k => k.Key,
                    v => v.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>()
                );
                return BadRequest(new { success = false, message = "Validation failed.", errors });
            }

            int? currentUserId = Utility.Helper.GetUserId(User);
            var existing = await _supplierService.GetSupplierById(supplier.SupplierId, currentUserId);
            if (existing == null)
            {
                return NotFound(new { success = false, message = "Supplier not found." });
            }

            supplier.UserId = currentUserId;
            var success = await _supplierService.SaveSupplier(supplier);
            if (!success)
            {
                return BadRequest(new { success = false, message = "Unable to update supplier." });
            }

            return Ok(new
            {
                success = true,
                message = "Supplier updated successfully.",
                data = supplier,
                redirectUrl = "/Supplier"
            });
        }

        [HttpPost]
        [Route("supplier_status_update")]
        [Route("toggle-status")]
        [Route("toggle-status/{id}")]
        [Route("ToggleStatus/{id}")]
        public async Task<IActionResult> ToggleStatus([FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] ToggleStatusReq? model, [FromQuery(Name = "id")] int? queryId = null, int? id = null)
        {
            long targetId = (queryId ?? 0) > 0 ? queryId!.Value : ((id ?? 0) > 0 ? id!.Value : (model != null && model.Id > 0 ? model.Id : 0));
            if (targetId <= 0)
            {
                return BadRequest(new { success = false, message = "Invalid supplier identifier." });
            }

            try
            {
                int? currentUserId = Utility.Helper.GetUserId(User);
                var newStatus = await _supplierService.ToggleStatus(targetId, currentUserId);
                var dctData = new Dictionary<string, object?>
                {
                    { "success", true },
                    { "isActive", newStatus },
                    { "message", $"Supplier status updated to {(newStatus ? "Active" : "Inactive")}." },
                    { "Data", new { IsActive = newStatus, SupplierId = targetId } },
                    { "Status", new TranStatus { Code = 0, ReturnMessage = "Success." } }
                };
                return Ok(dctData);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Error updating status: " + ex.Message });
            }
        }

        [HttpDelete]
        [HttpPost]
        [Route("supplier_delete")]
        [Route("{id:long}")]
        [Route("delete-supplier")]
        [Route("delete-supplier/{id}")]
        [Route("Delete/{id}")]
        public async Task<IActionResult> Delete([FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] DeleteReq? model = null, [FromQuery(Name = "id")] long? queryId = null, long id = 0)
        {
            long targetId = (queryId ?? 0) > 0 ? queryId!.Value : (id > 0 ? id : (model != null && model.Id > 0 ? model.Id : 0));
            if (targetId <= 0) return BadRequest(new { success = false, message = "Invalid supplier identifier." });

            int? currentUserId = Utility.Helper.GetUserId(User);
            var success = await _supplierService.DeleteSupplier(targetId, currentUserId);
            return Ok(new
            {
                success,
                message = success ? "Supplier deleted successfully." : "Failed to delete supplier."
            });
        }
    }
}
