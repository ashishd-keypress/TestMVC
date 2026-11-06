using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TestMVC.Interface;
using TestMVC.Models;

namespace TestMVC.Controllers
{
    [Authorize]
    public class SupplierController : Controller
    {
        private readonly ISupplierService _supplierService;

        public SupplierController(ISupplierService supplierService)
        {
            _supplierService = supplierService;
        }

        // GET: /Supplier
        [HttpGet]
        public async Task<IActionResult> Index(string? search)
        {
            var suppliers = await _supplierService.GetSuppliers(search);
            ViewBag.Search = search;
            return View(suppliers);
        }

        // GET: /Supplier/Create
        [HttpGet]
        public IActionResult Create()
        {
            return View(new Supplier());
        }

        // POST: /Supplier/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Supplier supplier)
        {
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
                return View(supplier);
            }

            var success = await _supplierService.SaveSupplier(supplier);
            if (success)
            {
                TempData["Success"] = "Supplier added successfully.";
                return RedirectToAction(nameof(Index));
            }

            ModelState.AddModelError(string.Empty, "Unable to save supplier. Please check details.");
            return View(supplier);
        }

        // GET: /Supplier/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(long id)
        {
            var supplier = await _supplierService.GetSupplierById(id);
            if (supplier == null)
            {
                TempData["Error"] = "Supplier not found.";
                return RedirectToAction(nameof(Index));
            }

            return View(supplier);
        }

        // POST: /Supplier/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(long id, Supplier supplier)
        {
            if (id != supplier.SupplierId) return BadRequest();

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
                return View(supplier);
            }

            var success = await _supplierService.SaveSupplier(supplier);
            if (success)
            {
                TempData["Success"] = "Supplier updated successfully.";
                return RedirectToAction(nameof(Index));
            }

            ModelState.AddModelError(string.Empty, "Unable to update supplier.");
            return View(supplier);
        }

        // POST: /Supplier/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(long id, [FromForm] long? supplierId)
        {
            long targetId = id > 0 ? id : (supplierId ?? 0);
            if (targetId <= 0) return Json(new { success = false, message = "Invalid supplier identifier." });

            var success = await _supplierService.DeleteSupplier(targetId);
            bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                          Request.Headers.Accept.ToString().Contains("application/json") ||
                          Request.ContentType?.Contains("application/json") == true ||
                          Request.ContentType?.Contains("application/x-www-form-urlencoded") == true;

            if (isAjax)
            {
                return Json(new { 
                    success, 
                    message = success ? "Supplier deleted successfully." : "Failed to delete supplier." 
                });
            }

            if (success) TempData["Success"] = "Supplier deleted successfully.";
            else TempData["Error"] = "Failed to delete supplier.";

            return RedirectToAction(nameof(Index));
        }

        // POST: /Supplier/ToggleStatus/5
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> ToggleStatus(long id, [FromForm] long? supplierId)
        {
            long targetId = id > 0 ? id : (supplierId ?? 0);
            if (targetId <= 0) return Json(new { success = false, message = "Invalid supplier identifier." });

            var newStatus = await _supplierService.ToggleStatus(targetId);
            return Json(new { 
                success = true, 
                isActive = newStatus, 
                message = $"Supplier status updated to {(newStatus ? "Active" : "Inactive")}." 
            });
        }
    }
}
