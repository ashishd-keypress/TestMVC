using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TestMVC.Interface;
using TestMVC.Models;
using TestMVC.Models.ReqRes;
using Utility;

namespace TestMVC.Controllers
{
    [Authorize]
    public class ProductController : Controller
    {
        private readonly IProductService _productService;
        private readonly ICategoryService _categoryService;
        private readonly IBrandService _brandService;
        private readonly IWebHostEnvironment _env;
        private const int PageSize = 10;

        public ProductController(
            IProductService productService,
            ICategoryService categoryService,
            IBrandService brandService,
            IWebHostEnvironment env)
        {
            _productService = productService;
            _categoryService = categoryService;
            _brandService = brandService;
            _env = env;
        }

        // GET: /Product
        [HttpGet]
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
                Image = p.Image ?? p.ImageUrl,
                ImageUrl = p.Image ?? p.ImageUrl,
                Price = p.Price,
                MRP = p.MRP,
                SellingPrice = p.SellingPrice,
                Size = p.Size,
                RAM = p.RAM,
                Storage = p.Storage,
                Color = p.Color,
                SKU = p.SKU,
                Barcode = p.Barcode,
                WarrantyMonths = p.WarrantyMonths,
                TrackIMEI = p.TrackIMEI,
                StockQuantity = p.StockQuantity,
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
                    image = p.Image ?? p.ImageUrl,
                    imageUrl = p.Image ?? p.ImageUrl,
                    brand = p.Brand,
                    category = p.Category,
                    price = p.Price,
                    mrp = p.MRP,
                    sellingPrice = p.SellingPrice,
                    size = p.Size,
                    ram = p.RAM,
                    storage = p.Storage,
                    color = p.Color,
                    sku = p.SKU,
                    barcode = p.Barcode,
                    warrantyMonths = p.WarrantyMonths,
                    trackImei = p.TrackIMEI,
                    stockQuantity = p.StockQuantity,
                    isActive = p.IsActive,
                    userId = p.UserId,
                    createdDate = p.CreatedDate.HasValue ? p.CreatedDate.Value.ToString("yyyy-MM-dd hh:mm tt") : "-"
                }),
                pageNo = pageNo,
                hasNext = (pageNo * PageSize) < totalRecords
            });
        }

        // GET: /Product/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(int id)
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
                Image = res.Image ?? res.ImageUrl,
                ImageUrl = res.Image ?? res.ImageUrl,
                BrandId = res.BrandId ?? 0,
                Brand = res.Brand,
                CategoryId = res.CategoryId ?? 0,
                Category = res.Category,
                Price = res.Price,
                MRP = res.MRP,
                SellingPrice = res.SellingPrice,
                Size = res.Size,
                RAM = res.RAM,
                Storage = res.Storage,
                Color = res.Color,
                SKU = res.SKU,
                Barcode = res.Barcode,
                WarrantyMonths = res.WarrantyMonths,
                TrackIMEI = res.TrackIMEI,
                StockQuantity = res.StockQuantity,
                IsActive = res.IsActive,
                UserId = res.UserId,
                CreatedDate = res.CreatedDate
            };

            return View(product);
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
            return View(new Product { WarrantyMonths = 12, TrackIMEI = true });
        }

        // POST: /Product/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product)
        {
            // Mandatory Image Upload on Create
            var (imgValid, imgErr) = FileHelper.ValidateImage(product.ImageFile, isRequired: true);
            if (!imgValid)
            {
                ModelState.AddModelError("ImageFile", imgErr!);
            }

            if (!ModelState.IsValid)
            {
                await LoadDropdownsAsync(product.CategoryId, product.BrandId);
                return View(product);
            }

            string? savedImageUrl = null;
            if (product.ImageFile != null && product.ImageFile.Length > 0)
            {
                savedImageUrl = await FileHelper.SaveImageAsync(product.ImageFile, _env, "products");
            }

            int? currentUserId = Utility.Helper.GetUserId(User);
            var saveReq = new SaveProductReq
            {
                ProductId = 0,
                Name = product.Name,
                Image = savedImageUrl,
                ImageUrl = savedImageUrl,
                BrandId = product.BrandId,
                CategoryId = product.CategoryId,
                Price = product.Price,
                MRP = product.MRP,
                SellingPrice = product.SellingPrice ?? product.Price,
                Size = product.Size,
                RAM = product.RAM,
                Storage = product.Storage,
                Color = product.Color,
                SKU = product.SKU,
                Barcode = product.Barcode,
                WarrantyMonths = product.WarrantyMonths > 0 ? product.WarrantyMonths : 12,
                TrackIMEI = product.TrackIMEI,
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
                if (!string.IsNullOrEmpty(savedImageUrl))
                {
                    FileHelper.DeleteImage(savedImageUrl, _env);
                }

                ModelState.AddModelError(string.Empty, "Unable to save product. Please check the entered details and try again.");
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
                Image = res.Image ?? res.ImageUrl,
                ImageUrl = res.Image ?? res.ImageUrl,
                BrandId = res.BrandId ?? 0,
                Brand = res.Brand,
                CategoryId = res.CategoryId ?? 0,
                Category = res.Category,
                Price = res.Price,
                MRP = res.MRP,
                SellingPrice = res.SellingPrice,
                Size = res.Size,
                RAM = res.RAM,
                Storage = res.Storage,
                Color = res.Color,
                SKU = res.SKU,
                Barcode = res.Barcode,
                WarrantyMonths = res.WarrantyMonths,
                TrackIMEI = res.TrackIMEI,
                StockQuantity = res.StockQuantity,
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

            // Image validation: Must either have an existing image or a newly uploaded image
            string? existingImg = existing.Image ?? existing.ImageUrl;
            bool hasExisting = !string.IsNullOrEmpty(existingImg);
            var (imgValid, imgErr) = FileHelper.ValidateImage(product.ImageFile, isRequired: !hasExisting);
            if (!imgValid)
            {
                ModelState.AddModelError("ImageFile", imgErr!);
            }

            if (!ModelState.IsValid)
            {
                product.Image = existingImg;
                product.ImageUrl = existingImg;
                await LoadDropdownsAsync(product.CategoryId, product.BrandId);
                return View(product);
            }

            string finalImageUrl = existingImg ?? string.Empty;
            if (product.ImageFile != null && product.ImageFile.Length > 0)
            {
                finalImageUrl = await FileHelper.SaveImageAsync(product.ImageFile, _env, "products");
                FileHelper.DeleteImage(existingImg, _env);
            }

            var updateReq = new SaveProductReq
            {
                ProductId = product.ProductId,
                Name = product.Name,
                Image = finalImageUrl,
                ImageUrl = finalImageUrl,
                BrandId = product.BrandId > 0 ? product.BrandId : existing.BrandId,
                CategoryId = product.CategoryId > 0 ? product.CategoryId : existing.CategoryId,
                Price = product.Price,
                MRP = product.MRP,
                SellingPrice = product.SellingPrice ?? product.Price,
                Size = product.Size,
                RAM = product.RAM,
                Storage = product.Storage,
                Color = product.Color,
                SKU = product.SKU,
                Barcode = product.Barcode,
                WarrantyMonths = product.WarrantyMonths > 0 ? product.WarrantyMonths : 12,
                TrackIMEI = product.TrackIMEI,
                IsActive = product.IsActive,
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
                ModelState.AddModelError(string.Empty, "Unable to update product. Please check the entered details and try again.");
                product.Image = existingImg;
                product.ImageUrl = existingImg;
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
                if (IsAjaxRequest())
                    return Json(new { success = false, message = "Invalid Product ID." });

                return BadRequest();
            }

            int? currentUserId = Utility.Helper.GetUserId(User);
            var (res, _, _) = await _productService.DeleteProduct(new DeleteReq { Id = id, UserId = currentUserId });

            if (IsAjaxRequest())
            {
                return Json(new
                {
                    success = res.Success,
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

        private async Task LoadDropdownsAsync(int? selectedCategory = null, int? selectedBrand = null)
        {
            int? currentUserId = Utility.Helper.GetUserId(User);

            var (catsRes, _, _) = await _categoryService.GetCategory(new GetCategoryReq { UserId = currentUserId, IsActive = true, PageSize = 1000 });
            var (brandsRes, _, _) = await _brandService.GetBrand(new GetBrandReq { UserId = currentUserId, IsActive = true, PageSize = 1000 });

            ViewBag.Categories = new SelectList(catsRes, "CategoryId", "CategoryName", selectedCategory);
            ViewBag.Brands = new SelectList(brandsRes, "BrandId", "BrandName", selectedBrand);
        }

        private bool IsAjaxRequest()
        {
            return Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                   Request.Headers.Accept.ToString().Contains("application/json");
        }
    }
}
