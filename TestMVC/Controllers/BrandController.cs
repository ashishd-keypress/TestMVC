using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using TestMVC.Interface;
using TestMVC.Models;

namespace TestMVC.Controllers
{
    public class BrandController : Controller
    {
        private readonly IBrandRepository _repo;

        public BrandController(IBrandRepository repo)
        {
            _repo = repo;
        }

        // GET: /Brand
        public async Task<IActionResult> Index()
        {
            var brands = await _repo.GetAllBrandsAsync();
            return View(brands);
        }

        // GET: /Brand/Create
        [HttpGet]
        public IActionResult Create()
        {
            return View(new Brand());
        }

        // POST: /Brand/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Brand brand)
        {
            if (!ModelState.IsValid)
            {
                return View(brand);
            }

            try
            {
                await _repo.AddBrandAsync(brand);
                TempData["Success"] = "Brand added successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (SqlException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(brand);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "An unexpected error occurred: " + ex.Message);
                return View(brand);
            }
        }

        // GET: /Brand/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            if (id <= 0)
            {
                return BadRequest();
            }

            var brand = await _repo.GetBrandByIdAsync(id);
            if (brand == null)
            {
                TempData["Error"] = "Brand not found.";
                return RedirectToAction(nameof(Index));
            }

            return View(brand);
        }

        // POST: /Brand/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Brand brand)
        {
            if (id != brand.BrandId)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                return View(brand);
            }

            try
            {
                await _repo.UpdateBrandAsync(brand);
                TempData["Success"] = "Brand updated successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (SqlException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(brand);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "An unexpected error occurred: " + ex.Message);
                return View(brand);
            }
        }

        // POST: /Brand/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            if (id <= 0)
            {
                return BadRequest();
            }

            try
            {
                await _repo.DeleteBrandAsync(id);
                TempData["Success"] = "Brand deleted successfully.";
            }
            catch (SqlException ex)
            {
                TempData["Error"] = ex.Message;
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while deleting: " + ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
