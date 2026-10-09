using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using TestMVC.Interface;
using TestMVC.Models;

namespace TestMVC.Controllers
{
    public class ProductController : Controller
    {
        private const int PageSize = 10;
        private readonly IProductRepository _repo;

        public ProductController(IProductRepository repo)
        {
            _repo = repo;
        }

        // GET: /Product?search=abc&pageNo=1
        public async Task<IActionResult> Index(string? search, int pageNo = 1)
        {
            if (pageNo < 1) pageNo = 1;

            var products = await _repo.GetProductsAsync(pageNo, PageSize, search);

            ViewBag.Search = search;
            ViewBag.PageNo = pageNo;
            ViewBag.HasNext = products.Count == PageSize;

            return View(products);
        }

        // GET: /Product/Create
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadDropdownsAsync();
            return View(new Product());
        }

        // POST: /Product/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product)
        {
            if (!ModelState.IsValid)
            {
                await LoadDropdownsAsync(product.CategoryId, product.BrandId);
                return View(product);
            }

            try
            {
                await _repo.AddProductAsync(product);
                TempData["Success"] = "Product added successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (SqlException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                await LoadDropdownsAsync(product.CategoryId, product.BrandId);
                return View(product);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "An unexpected error occurred: " + ex.Message);
                await LoadDropdownsAsync(product.CategoryId, product.BrandId);
                return View(product);
            }
        }

        // GET: /Product/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            if (id <= 0)
            {
                return BadRequest();
            }

            var product = await _repo.GetProductByIdAsync(id);
            if (product == null)
            {
                TempData["Error"] = "Product not found.";
                return RedirectToAction(nameof(Index));
            }

            await LoadDropdownsAsync(product.CategoryId, product.BrandId);
            return View(product);
        }

        // POST: /Product/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Product product)
        {
            if (id != product.ProductId)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                await LoadDropdownsAsync(product.CategoryId, product.BrandId);
                return View(product);
            }

            try
            {
                await _repo.UpdateProductAsync(product);
                TempData["Success"] = "Product updated successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (SqlException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                await LoadDropdownsAsync(product.CategoryId, product.BrandId);
                return View(product);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "An unexpected error occurred: " + ex.Message);
                await LoadDropdownsAsync(product.CategoryId, product.BrandId);
                return View(product);
            }
        }

        // POST: /Product/Delete/5
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
                await _repo.DeleteProductAsync(id);
                TempData["Success"] = "Product deleted successfully.";
            }
            catch (SqlException ex)
            {
                TempData["Error"] = ex.Message;
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An unexpected error occurred while deleting: " + ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task LoadDropdownsAsync(int? selectedCategoryId = null, int? selectedBrandId = null)
        {
            var categories = await _repo.GetCategoriesAsync();
            var brands = await _repo.GetBrandsAsync();

            ViewBag.Categories = new SelectList(categories, "CategoryId", "CategoryName", selectedCategoryId);
            ViewBag.Brands = new SelectList(brands, "BrandId", "BrandName", selectedBrandId);
        }
    }
}
