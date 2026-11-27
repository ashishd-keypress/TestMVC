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
    public class CustomerController : ControllerBase
    {
        private readonly ICustomerService _customerService;

        public CustomerController(ICustomerService customerService)
        {
            _customerService = customerService;
        }

        [HttpGet]
        [Route("")]
        [Route("customer_list_get")]
        [Route("search")]
        public async Task<IActionResult> Search([FromQuery] string? search)
        {
            int? currentUserId = Utility.Helper.GetUserId(User);
            var customers = await _customerService.GetCustomers(search, currentUserId);

            return Ok(new
            {
                success = true,
                data = customers.Select(c => new
                {
                    customerId = c.CustomerId,
                    customerName = c.CustomerName,
                    phone = c.Phone,
                    email = c.Email,
                    gstNumber = c.GSTNumber,
                    address = c.Address,
                    openingBalance = c.OpeningBalance,
                    currentBalance = c.CurrentBalance,
                    currentBalanceFormatted = c.CurrentBalance.ToString("N2"),
                    isActive = c.IsActive,
                    userId = c.UserId
                }),
                totalRecords = customers.Count
            });
        }

        [HttpGet]
        [Route("customer_detail_get")]
        [Route("{id:long}")]
        public async Task<IActionResult> GetById([FromQuery(Name = "id")] long? queryId = null, long? id = null)
        {
            long targetId = (queryId ?? 0) > 0 ? queryId!.Value : (id ?? 0);
            if (targetId <= 0) return BadRequest(new { success = false, message = "Invalid customer identifier." });

            int? currentUserId = Utility.Helper.GetUserId(User);
            var customer = await _customerService.GetCustomerById(targetId, currentUserId);
            if (customer == null)
            {
                return NotFound(new { success = false, message = "Customer not found." });
            }

            return Ok(new { success = true, data = customer });
        }

        [HttpPost]
        [Route("")]
        [Route("customer_detail_create")]
        [Route("create")]
        public async Task<IActionResult> Create([FromBody] Customer customer)
        {
            if (customer == null) return BadRequest(new { success = false, message = "Request body is required." });

            if (!string.IsNullOrWhiteSpace(customer.Phone))
            {
                customer.Phone = Regex.Replace(customer.Phone, @"[^0-9]", "");
            }

            if (string.IsNullOrWhiteSpace(customer.Phone) || !Regex.IsMatch(customer.Phone, @"^[0-9]{10,15}$"))
            {
                ModelState.AddModelError(nameof(customer.Phone), "Phone number must contain only digits (10 to 15 digits).");
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
            customer.CustomerId = 0;
            customer.UserId = currentUserId;

            var id = await _customerService.SaveCustomer(customer);
            if (id <= 0)
            {
                return BadRequest(new { success = false, message = "Unable to save customer." });
            }

            customer.CustomerId = id;
            return StatusCode(201, new
            {
                success = true,
                message = "Customer added successfully.",
                data = customer,
                redirectUrl = "/Customer"
            });
        }

        [HttpPut]
        [HttpPost]
        [Route("customer_detail_update")]
        [Route("{id:long}")]
        [Route("update/{id:long}")]
        [Route("update")]
        public async Task<IActionResult> Update([FromBody] Customer customer, [FromQuery(Name = "id")] long? queryId = null, long? id = null)
        {
            if (customer == null) return BadRequest(new { success = false, message = "Request body is required." });

            if ((queryId ?? 0) > 0)
            {
                customer.CustomerId = queryId!.Value;
            }
            else if (id.HasValue && id.Value > 0)
            {
                customer.CustomerId = id.Value;
            }

            if (customer.CustomerId <= 0)
            {
                return BadRequest(new { success = false, message = "Valid CustomerId is required." });
            }

            if (!string.IsNullOrWhiteSpace(customer.Phone))
            {
                customer.Phone = Regex.Replace(customer.Phone, @"[^0-9]", "");
            }

            if (string.IsNullOrWhiteSpace(customer.Phone) || !Regex.IsMatch(customer.Phone, @"^[0-9]{10,15}$"))
            {
                ModelState.AddModelError(nameof(customer.Phone), "Phone number must contain only digits (10 to 15 digits).");
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
            var existing = await _customerService.GetCustomerById(customer.CustomerId, currentUserId);
            if (existing == null)
            {
                return NotFound(new { success = false, message = "Customer not found." });
            }

            customer.UserId = currentUserId;
            var resId = await _customerService.SaveCustomer(customer);
            if (resId <= 0)
            {
                return BadRequest(new { success = false, message = "Unable to update customer." });
            }

            return Ok(new
            {
                success = true,
                message = "Customer updated successfully.",
                data = customer,
                redirectUrl = "/Customer"
            });
        }

        [HttpPost]
        [Route("customer_status_update")]
        [Route("toggle-status")]
        [Route("toggle-status/{id}")]
        [Route("ToggleStatus/{id}")]
        public async Task<IActionResult> ToggleStatus([FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] ToggleStatusReq? model, [FromQuery(Name = "id")] int? queryId = null, int? id = null)
        {
            long targetId = (queryId ?? 0) > 0 ? queryId!.Value : ((id ?? 0) > 0 ? id!.Value : (model != null && model.Id > 0 ? model.Id : 0));
            if (targetId <= 0)
            {
                return BadRequest(new { success = false, message = "Invalid customer identifier." });
            }

            try
            {
                int? currentUserId = Utility.Helper.GetUserId(User);
                var newStatus = await _customerService.ToggleStatus(targetId, currentUserId);
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

        [HttpDelete]
        [HttpPost]
        [Route("customer_delete")]
        [Route("{id:long}")]
        [Route("delete-customer")]
        [Route("delete-customer/{id}")]
        [Route("Delete/{id}")]
        public async Task<IActionResult> Delete([FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] DeleteReq? model = null, [FromQuery(Name = "id")] long? queryId = null, long id = 0)
        {
            long targetId = (queryId ?? 0) > 0 ? queryId!.Value : (id > 0 ? id : (model != null && model.Id > 0 ? model.Id : 0));
            if (targetId <= 0) return BadRequest(new { success = false, message = "Invalid customer identifier." });

            int? currentUserId = Utility.Helper.GetUserId(User);
            var success = await _customerService.DeleteCustomer(targetId, currentUserId);
            return Ok(new
            {
                success,
                message = success ? "Customer deleted successfully." : "Failed to delete customer."
            });
        }
    }
}
