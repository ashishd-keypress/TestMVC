using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TestMVC.Interface;
using TestMVC.Models;
using TestMVC.Models.ReqRes;

namespace TestMVC.Controllers
{
    public class ProductController : Controller
    {
        private const int PageSize = 10;
        private readonly IProductService _productService;
        private readonly IBrandService _brandService;
        private readonly ICategoryService _categoryService;

        public ProductController(
            IProductService productService,
            IBrandService brandService,
            ICategoryService categoryService)
        {
            _productService = productService;
            _brandService = brandService;
            _categoryService = categoryService;
        }

        // GET: /Product?search=abc&pageNo=1
        public async Task<IActionResult> Index(string? search, int pageNo = 1)
        {
            if (pageNo < 1) pageNo = 1;

            int? currentUserId = Utility.Helper.GetUserId(User);
            var req = new GetProductReq
            {
                SearchText = search,
                PageNo = pageNo,
                PageSize = PageSize,
                UserId = currentUserId
            };

            var (productsRes, totalRecords, _) = await _productService.GetProduct(req);

            var products = productsRes.Select(p => new Product
            {
                ProductId = p.ProductId,
                Name = p.Name,
                Price = p.Price,
                Size = p.Size,
                BrandId = p.BrandId ?? 0,
                Brand = p.Brand,
                CategoryId = p.CategoryId ?? 0,
                Category = p.Category,
                IsActive = p.IsActive,
                UserId = p.UserId,
                CreatedDate = p.CreatedDate
            }).ToList();

            ViewBag.Search = search;
            ViewBag.PageNo = pageNo;
            ViewBag.HasNext = (pageNo * PageSize) < totalRecords;

            return View(products);
        }

        // GET: /Product/SearchApi?search=abc&pageNo=1
        [HttpGet]
        public async Task<IActionResult> SearchApi(string? search, int pageNo = 1)
        {
            if (pageNo < 1) pageNo = 1;

            int? currentUserId = Utility.Helper.GetUserId(User);
            var req = new GetProductReq
            {
                SearchText = search,
                PageNo = pageNo,
                PageSize = PageSize,
                UserId = currentUserId
            };

            var (productsRes, totalRecords, strErr) = await _productService.GetProduct(req);

            return Json(new
            {
                success = string.IsNullOrEmpty(strErr),
                data = productsRes.Select(p => new
                {
                    productId = p.ProductId,
                    name = p.Name,
                    brand = p.Brand,
                    category = p.Category,
                    price = p.Price,
                    size = p.Size,
                    isActive = p.IsActive,
                    userId = p.UserId,
                    createdDate = p.CreatedDate.HasValue ? p.CreatedDate.Value.ToString("yyyy-MM-dd HH:mm") : "-"
                }),
                pageNo = pageNo,
                hasNext = (pageNo * PageSize) < totalRecords
            });
        }

        // POST: /Product/ToggleStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            if (id <= 0)
            {
                if (IsAjaxRequest())
                    return Json(new { success = false, message = "Invalid Product ID." });

                return BadRequest();
            }

            int? currentUserId = Utility.Helper.GetUserId(User);
            var (res, _, strErr) = await _productService.ToggleStatus(new ToggleStatusReq { Id = id, UserId = currentUserId });

            if (IsAjaxRequest())
            {
                return Json(new
                {
                    success = res.Success,
                    isActive = res.IsActive,
                    message = res.Message
                });
            }

            if (res.Success)
            {
                TempData["Success"] = res.Message;
            }
            else
            {
                TempData["Error"] = res.Message;
            }

            return RedirectToAction(nameof(Index));
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

            int? currentUserId = Utility.Helper.GetUserId(User);
            var saveReq = new SaveProductReq
            {
                ProductId = 0,
                Name = product.Name,
                BrandId = product.BrandId,
                CategoryId = product.CategoryId,
                Price = product.Price,
                Size = product.Size,
                IsActive = product.IsActive,
                UserId = currentUserId
            };

            var (success, _, err) = await _productService.SaveProduct(saveReq);

            if (success)
            {
                TempData["Success"] = "Product added successfully.";
                return RedirectToAction(nameof(Index));
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Failed to save product: " + err);
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

            int? currentUserId = Utility.Helper.GetUserId(User);
            var (res, _, _) = await _productService.GetProductById(new GetProductByIdReq { ProductId = id, UserId = currentUserId });
            if (res == null)
            {
                TempData["Error"] = "Product not found.";
                return RedirectToAction(nameof(Index));
            }

            var product = new Product
            {
                ProductId = res.ProductId,
                Name = res.Name,
                BrandId = res.BrandId ?? 0,
                Brand = res.Brand,
                CategoryId = res.CategoryId ?? 0,
                Category = res.Category,
                Price = res.Price,
                Size = res.Size,
                IsActive = res.IsActive,
                UserId = res.UserId,
                CreatedDate = res.CreatedDate
            };

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

            int? currentUserId = Utility.Helper.GetUserId(User);
            var (existing, _, _) = await _productService.GetProductById(new GetProductByIdReq { ProductId = id, UserId = currentUserId });
            if (existing == null)
            {
                TempData["Error"] = "Product not found.";
                return RedirectToAction(nameof(Index));
            }

            product.BrandId = existing.BrandId ?? 0;
            product.CategoryId = existing.CategoryId ?? 0;

            if (!ModelState.IsValid)
            {
                await LoadDropdownsAsync(existing.CategoryId, existing.BrandId);
                return View(product);
            }

            var updateReq = new SaveProductReq
            {
                ProductId = product.ProductId,
                Name = product.Name,
                Price = product.Price,
                Size = product.Size,
                IsActive = product.IsActive,
                BrandId = existing.BrandId,
                CategoryId = existing.CategoryId,
                UserId = currentUserId
            };

            var (success, _, err) = await _productService.SaveProduct(updateReq);

            if (success)
            {
                TempData["Success"] = "Product updated successfully.";
                return RedirectToAction(nameof(Index));
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Failed to update product: " + err);
                await LoadDropdownsAsync(existing.CategoryId, existing.BrandId);
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
                if (IsAjaxRequest())
                    return Json(new { success = false, message = "Invalid Product ID." });

                return BadRequest();
            }

            int? currentUserId = Utility.Helper.GetUserId(User);
            var (res, _, err) = await _productService.DeleteProduct(new DeleteReq { Id = id, UserId = currentUserId });

            if (IsAjaxRequest())
            {
                return Json(new { success = res.Success, message = res.Message });
            }

            if (res.Success)
            {
                TempData["Success"] = res.Message;
            }
            else
            {
                TempData["Error"] = res.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        private bool IsAjaxRequest()
        {
            return Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                   Request.Headers.Accept.ToString().Contains("application/json");
        }

        private async Task LoadDropdownsAsync(int? selectedCategoryId = null, int? selectedBrandId = null)
        {
            int? currentUserId = Utility.Helper.GetUserId(User);

            // Only load ACTIVE brands and categories (IsActive = true) created by current user for product dropdowns
            var (categoriesRes, _, _) = await _categoryService.GetCategory(new GetCategoryReq { PageSize = 500, IsActive = true, UserId = currentUserId });
            var (brandsRes, _, _) = await _brandService.GetBrand(new GetBrandReq { PageSize = 500, IsActive = true, UserId = currentUserId });

            // If editing an existing product whose category/brand was previously assigned, keep it in the dropdown list
            if (selectedCategoryId.HasValue && selectedCategoryId.Value > 0 && !categoriesRes.Any(c => c.CategoryId == selectedCategoryId.Value))
            {
                var (selectedCat, _, _) = await _categoryService.GetCategoryById(new GetCategoryByIdReq { CategoryId = selectedCategoryId.Value, UserId = currentUserId });
                if (selectedCat != null)
                {
                    categoriesRes.Add(selectedCat);
                }
            }

            if (selectedBrandId.HasValue && selectedBrandId.Value > 0 && !brandsRes.Any(b => b.BrandId == selectedBrandId.Value))
            {
                var (selectedB, _, _) = await _brandService.GetBrandById(new GetBrandByIdReq { BrandId = selectedBrandId.Value, UserId = currentUserId });
                if (selectedB != null)
                {
                    brandsRes.Add(selectedB);
                }
            }

            ViewBag.Categories = new SelectList(categoriesRes, "CategoryId", "CategoryName", selectedCategoryId);
            ViewBag.Brands = new SelectList(brandsRes, "BrandId", "BrandName", selectedBrandId);
        }
    }
}
