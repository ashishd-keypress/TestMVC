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
    public class CustomerController : ControllerBase
    {
        private readonly ICustomerService _customerService;

        public CustomerController(ICustomerService customerService)
        {
            _customerService = customerService;
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
                return BadRequest(new { success = false, message = "Invalid customer identifier." });
            }

            try
            {
                var newStatus = await _customerService.ToggleStatus(targetId);
                var dctData = new Dictionary<string, object?>
                {
                    { "success", true },
                    { "isActive", newStatus },
                    { "message", $"Customer status updated to {(newStatus ? "Active" : "Inactive")}." },
                    { "Data", new { IsActive = newStatus, CustomerId = targetId } },
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
        [Route("delete-customer/{id}")]
        [Route("Delete/{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            if (id <= 0) return BadRequest(new { success = false, message = "Invalid customer identifier." });

            var success = await _customerService.DeleteCustomer(id);
            return Ok(new { 
                success, 
                message = success ? "Customer deleted successfully." : "Failed to delete customer." 
            });
        }
    }
}
