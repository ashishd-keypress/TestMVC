using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;
using TestMVC.Interface;
using TestMVC.Models;
using TestMVC.Models.ReqRes;
using Utility;

namespace TestMVC.Controllers
{
    [Authorize]
    public class BrandController : Controller
    {
        private readonly IBrandService _brandService;
        private readonly IWebHostEnvironment _env;

        public BrandController(IBrandService brandService, IWebHostEnvironment env)
        {
            _brandService = brandService;
            _env = env;
        }

        // GET: /Brand
        public async Task<IActionResult> Index()
        {
            int? currentUserId = Utility.Helper.GetUserId(User);
            var (brandsRes, _, _) = await _brandService.GetBrand(new GetBrandReq { PageSize = 100, UserId = currentUserId });
            var brands = brandsRes.Select(b => new Brand
            {
                BrandId = b.BrandId,
                BrandName = b.BrandName,
                ImageUrl = b.ImageUrl,
                IsActive = b.IsActive,
                UserId = b.UserId,
                CreatedDate = b.CreatedDate
            }).ToList();

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
            // Mandatory Image Upload on Create
            var (imgValid, imgErr) = FileHelper.ValidateImage(brand.ImageFile, isRequired: true);
            if (!imgValid)
            {
                ModelState.AddModelError("ImageFile", imgErr!);
            }

            if (!ModelState.IsValid)
            {
                return View(brand);
            }

            string? savedImageUrl = null;
            if (brand.ImageFile != null && brand.ImageFile.Length > 0)
            {
                savedImageUrl = await FileHelper.SaveImageAsync(brand.ImageFile, _env, "brands");
            }

            int? currentUserId = Utility.Helper.GetUserId(User);
            var (success, _, err) = await _brandService.SaveBrand(new SaveBrandReq
            {
                BrandId = 0,
                BrandName = brand.BrandName,
                ImageUrl = savedImageUrl,
                IsActive = brand.IsActive,
                UserId = currentUserId
            });

            if (success)
            {
                TempData["Success"] = "Brand added successfully.";
                return RedirectToAction(nameof(Index));
            }

            // Clean up saved image if DB insert failed
            if (!string.IsNullOrEmpty(savedImageUrl))
            {
                FileHelper.DeleteImage(savedImageUrl, _env);
            }

            string errorMsg = !string.IsNullOrEmpty(err) && !err.StartsWith("{")
                ? err
                : "Unable to add brand. Please check the entered details and try again.";

            if (!string.IsNullOrEmpty(err) && err.Contains("already exists", StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError("BrandName", errorMsg);
            }
            else
            {
                ModelState.AddModelError(string.Empty, errorMsg);
            }
            return View(brand);
        }

        // GET: /Brand/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            if (id <= 0)
            {
                return BadRequest();
            }

            int? currentUserId = Utility.Helper.GetUserId(User);
            var (brandRes, _, _) = await _brandService.GetBrandById(new GetBrandByIdReq { BrandId = id, UserId = currentUserId });
            if (brandRes == null)
            {
                TempData["Error"] = "Brand not found.";
                return RedirectToAction(nameof(Index));
            }

            var brand = new Brand
            {
                BrandId = brandRes.BrandId,
                BrandName = brandRes.BrandName,
                ImageUrl = brandRes.ImageUrl,
                IsActive = brandRes.IsActive,
                UserId = brandRes.UserId,
                CreatedDate = brandRes.CreatedDate
            };

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

            int? currentUserId = Utility.Helper.GetUserId(User);
            var (existing, _, _) = await _brandService.GetBrandById(new GetBrandByIdReq { BrandId = id, UserId = currentUserId });
            if (existing == null)
            {
                TempData["Error"] = "Brand not found.";
                return RedirectToAction(nameof(Index));
            }

            // Image validation: Must either have an existing image or a newly uploaded image
            bool hasExisting = !string.IsNullOrEmpty(existing.ImageUrl);
            var (imgValid, imgErr) = FileHelper.ValidateImage(brand.ImageFile, isRequired: !hasExisting);
            if (!imgValid)
            {
                ModelState.AddModelError("ImageFile", imgErr!);
            }

            if (!ModelState.IsValid)
            {
                brand.ImageUrl = existing.ImageUrl;
                return View(brand);
            }

            string finalImageUrl = existing.ImageUrl ?? string.Empty;
            if (brand.ImageFile != null && brand.ImageFile.Length > 0)
            {
                finalImageUrl = await FileHelper.SaveImageAsync(brand.ImageFile, _env, "brands");
                FileHelper.DeleteImage(existing.ImageUrl, _env);
            }

            var (success, _, err) = await _brandService.SaveBrand(new SaveBrandReq
            {
                BrandId = brand.BrandId,
                BrandName = brand.BrandName,
                ImageUrl = finalImageUrl,
                IsActive = brand.IsActive,
                UserId = currentUserId
            });

            if (success)
            {
                TempData["Success"] = "Brand updated successfully.";
                return RedirectToAction(nameof(Index));
            }

            string errorMsg = !string.IsNullOrEmpty(err) && !err.StartsWith("{")
                ? err
                : "Unable to update brand. Please check the entered details and try again.";

            if (!string.IsNullOrEmpty(err) && err.Contains("already exists", StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError("BrandName", errorMsg);
            }
            else
            {
                ModelState.AddModelError(string.Empty, errorMsg);
            }
            brand.ImageUrl = existing.ImageUrl;
            return View(brand);
        }

        // GET: /Brand/ListApi
        [HttpGet]
        public async Task<IActionResult> ListApi()
        {
            int? currentUserId = Utility.Helper.GetUserId(User);
            var (brandsRes, _, _) = await _brandService.GetBrand(new GetBrandReq { PageSize = 100, UserId = currentUserId });
            return Json(new
            {
                success = true,
                data = brandsRes.Select(b => new
                {
                    brandId = b.BrandId,
                    brandName = b.BrandName,
                    imageUrl = b.ImageUrl,
                    isActive = b.IsActive,
                    userId = b.UserId,
                    createdDate = b.CreatedDate.HasValue ? b.CreatedDate.Value.ToString("yyyy-MM-dd hh:mm tt") : "-"
                })
            });
        }

        // POST: /Brand/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            if (id <= 0)
            {
                if (IsAjaxRequest())
                    return Json(new { success = false, message = "Invalid Brand ID." });

                return BadRequest();
            }

            int? currentUserId = Utility.Helper.GetUserId(User);
            // Optionally retrieve existing to delete image file upon deletion
            var (existing, _, _) = await _brandService.GetBrandById(new GetBrandByIdReq { BrandId = id, UserId = currentUserId });
            var (res, _, _) = await _brandService.DeleteBrand(new DeleteReq { Id = id, UserId = currentUserId });

            if (res.Success && existing != null && !string.IsNullOrEmpty(existing.ImageUrl))
            {
                FileHelper.DeleteImage(existing.ImageUrl, _env);
            }

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

        // POST: /Brand/ToggleStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            if (id <= 0)
            {
                if (IsAjaxRequest())
                    return Json(new { success = false, message = "Invalid Brand ID." });

                return BadRequest();
            }

            int? currentUserId = Utility.Helper.GetUserId(User);
            var (res, _, _) = await _brandService.ToggleStatus(new ToggleStatusReq { Id = id, UserId = currentUserId });

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

        private bool IsAjaxRequest()
        {
            return Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                   Request.Headers.Accept.ToString().Contains("application/json");
        }
    }
}
