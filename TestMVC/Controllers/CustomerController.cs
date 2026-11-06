using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using TestMVC.Interface;
using TestMVC.Models;

namespace TestMVC.Controllers
{
    [Authorize]
    public class CustomerController : Controller
    {
        private readonly ICustomerService _customerService;

        public CustomerController(ICustomerService customerService)
        {
            _customerService = customerService;
        }

        // GET: /Customer
        [HttpGet]
        public async Task<IActionResult> Index(string? search)
        {
            var customers = await _customerService.GetCustomers(search);
            ViewBag.Search = search;
            return View(customers);
        }

        // GET: /Customer/Create
        [HttpGet]
        public IActionResult Create()
        {
            return View(new Customer());
        }

        // POST: /Customer/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Customer customer)
        {
            if (!string.IsNullOrWhiteSpace(customer.Phone))
            {
                customer.Phone = System.Text.RegularExpressions.Regex.Replace(customer.Phone, @"[^0-9]", "");
            }

            if (string.IsNullOrWhiteSpace(customer.Phone) || !System.Text.RegularExpressions.Regex.IsMatch(customer.Phone, @"^[0-9]{10,15}$"))
            {
                ModelState.AddModelError(nameof(customer.Phone), "Phone number must contain only digits (10 to 15 digits).");
            }

            if (!ModelState.IsValid)
            {
                return View(customer);
            }

            var id = await _customerService.SaveCustomer(customer);
            if (id > 0)
            {
                TempData["Success"] = "Customer added successfully.";
                return RedirectToAction(nameof(Index));
            }

            ModelState.AddModelError(string.Empty, "Unable to save customer.");
            return View(customer);
        }

        // GET: /Customer/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(long id)
        {
            var customer = await _customerService.GetCustomerById(id);
            if (customer == null)
            {
                TempData["Error"] = "Customer not found.";
                return RedirectToAction(nameof(Index));
            }

            return View(customer);
        }

        // POST: /Customer/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, Customer customer)
        {
            if (id != customer.CustomerId) return BadRequest();

            if (!string.IsNullOrWhiteSpace(customer.Phone))
            {
                customer.Phone = System.Text.RegularExpressions.Regex.Replace(customer.Phone, @"[^0-9]", "");
            }

            if (string.IsNullOrWhiteSpace(customer.Phone) || !System.Text.RegularExpressions.Regex.IsMatch(customer.Phone, @"^[0-9]{10,15}$"))
            {
                ModelState.AddModelError(nameof(customer.Phone), "Phone number must contain only digits (10 to 15 digits).");
            }

            if (!ModelState.IsValid)
            {
                return View(customer);
            }

            var resId = await _customerService.SaveCustomer(customer);
            if (resId > 0)
            {
                TempData["Success"] = "Customer updated successfully.";
                return RedirectToAction(nameof(Index));
            }

            ModelState.AddModelError(string.Empty, "Unable to update customer.");
            return View(customer);
        }

        // POST: /Customer/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(long id, [FromForm] long? customerId)
        {
            long targetId = id > 0 ? id : (customerId ?? 0);
            if (targetId <= 0)
            {
                return Json(new { success = false, message = "Invalid customer identifier." });
            }

            var success = await _customerService.DeleteCustomer(targetId);
            bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                          Request.Headers.Accept.ToString().Contains("application/json") ||
                          Request.ContentType?.Contains("application/json") == true ||
                          Request.ContentType?.Contains("application/x-www-form-urlencoded") == true;

            if (isAjax)
            {
                return Json(new { 
                    success, 
                    message = success ? "Customer deleted successfully." : "Failed to delete customer." 
                });
            }

            if (success) TempData["Success"] = "Customer deleted successfully.";
            else TempData["Error"] = "Failed to delete customer.";

            return RedirectToAction(nameof(Index));
        }

        // POST: /Customer/ToggleStatus/5
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> ToggleStatus(long id, [FromForm] long? customerId)
        {
            long targetId = id > 0 ? id : (customerId ?? 0);
            if (targetId <= 0) return Json(new { success = false, message = "Invalid customer identifier." });

            var newStatus = await _customerService.ToggleStatus(targetId);
            return Json(new { 
                success = true, 
                isActive = newStatus, 
                message = $"Customer status updated to {(newStatus ? "Active" : "Inactive")}." 
            });
        }

        // GET: /Customer/SearchJson?query=...
        [HttpGet]
        public async Task<IActionResult> SearchJson(string query)
        {
            var list = await _customerService.GetCustomers(query);
            return Json(new { success = true, customers = list });
        }
    }
}
