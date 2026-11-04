using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;
using TestMVC.Interface;
using TestMVC.Models;
using TestMVC.Models.ReqRes;

namespace TestMVC.Controllers
{
    [Authorize]
    public class BrandController : Controller
    {
        private readonly IBrandService _brandService;

        public BrandController(IBrandService brandService)
        {
            _brandService = brandService;
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
            if (!ModelState.IsValid)
            {
                return View(brand);
            }

            int? currentUserId = Utility.Helper.GetUserId(User);
            var (success, _, err) = await _brandService.SaveBrand(new SaveBrandReq
            {
                BrandId = 0,
                BrandName = brand.BrandName,
                IsActive = brand.IsActive,
                UserId = currentUserId
            });

            if (success)
            {
                TempData["Success"] = "Brand added successfully.";
                return RedirectToAction(nameof(Index));
            }

            ModelState.AddModelError(string.Empty, "Failed to add brand: " + err);
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

            if (!ModelState.IsValid)
            {
                return View(brand);
            }

            int? currentUserId = Utility.Helper.GetUserId(User);
            var (success, _, err) = await _brandService.SaveBrand(new SaveBrandReq
            {
                BrandId = brand.BrandId,
                BrandName = brand.BrandName,
                IsActive = brand.IsActive,
                UserId = currentUserId
            });

            if (success)
            {
                TempData["Success"] = "Brand updated successfully.";
                return RedirectToAction(nameof(Index));
            }

            ModelState.AddModelError(string.Empty, "Failed to update brand: " + err);
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
                    isActive = b.IsActive,
                    userId = b.UserId,
                    createdDate = b.CreatedDate.HasValue ? b.CreatedDate.Value.ToString("yyyy-MM-dd HH:mm") : "-"
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
            var (res, _, _) = await _brandService.DeleteBrand(new DeleteReq { Id = id, UserId = currentUserId });

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
