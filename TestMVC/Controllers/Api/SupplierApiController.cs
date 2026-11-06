using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TestMVC.Interface;
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

        [HttpPost]
        [Route("toggle-status")]
        [Route("toggle-status/{id}")]
        [Route("ToggleStatus/{id}")]
        public async Task<IActionResult> ToggleStatus([FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] ToggleStatusReq? model, int? id)
        {
            long targetId = (id.HasValue && id.Value > 0) ? id.Value : (model != null && model.Id > 0 ? model.Id : 0);
            if (targetId <= 0)
            {
                return BadRequest(new { success = false, message = "Invalid supplier identifier." });
            }

            try
            {
                var newStatus = await _supplierService.ToggleStatus(targetId);
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

        [HttpPost]
        [Route("delete-supplier/{id}")]
        [Route("Delete/{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            if (id <= 0) return BadRequest(new { success = false, message = "Invalid supplier identifier." });

            var success = await _supplierService.DeleteSupplier(id);
            return Ok(new { 
                success, 
                message = success ? "Supplier deleted successfully." : "Failed to delete supplier." 
            });
        }
    }
}
