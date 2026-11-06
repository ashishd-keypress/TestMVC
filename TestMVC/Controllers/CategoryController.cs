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
    public class CategoryController : Controller
    {
        private readonly ICategoryService _categoryService;
        private readonly IWebHostEnvironment _env;

        public CategoryController(ICategoryService categoryService, IWebHostEnvironment env)
        {
            _categoryService = categoryService;
            _env = env;
        }

        // GET: /Category
        public async Task<IActionResult> Index()
        {
            int? currentUserId = Utility.Helper.GetUserId(User);
            var (categoriesRes, _, _) = await _categoryService.GetCategory(new GetCategoryReq { PageSize = 100, UserId = currentUserId });
            var categories = categoriesRes.Select(c => new Category
            {
                CategoryId = c.CategoryId,
                CategoryName = c.CategoryName,
                ImageUrl = c.ImageUrl,
                IsActive = c.IsActive,
                UserId = c.UserId,
                CreatedDate = c.CreatedDate
            }).ToList();

            return View(categories);
        }

        // GET: /Category/Create
        [HttpGet]
        public IActionResult Create()
        {
            return View(new Category());
        }

        // POST: /Category/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Category category)
        {
            // Mandatory Image Upload on Create
            var (imgValid, imgErr) = FileHelper.ValidateImage(category.ImageFile, isRequired: true);
            if (!imgValid)
            {
                ModelState.AddModelError("ImageFile", imgErr!);
            }

            if (!ModelState.IsValid)
            {
                return View(category);
            }

            string? savedImageUrl = null;
            if (category.ImageFile != null && category.ImageFile.Length > 0)
            {
                savedImageUrl = await FileHelper.SaveImageAsync(category.ImageFile, _env, "categories");
            }

            int? currentUserId = Utility.Helper.GetUserId(User);
            var (success, _, err) = await _categoryService.SaveCategory(new SaveCategoryReq
            {
                CategoryId = 0,
                CategoryName = category.CategoryName,
                ImageUrl = savedImageUrl,
                IsActive = category.IsActive,
                UserId = currentUserId
            });

            if (success)
            {
                TempData["Success"] = "Category added successfully.";
                return RedirectToAction(nameof(Index));
            }

            // Clean up saved image if DB insert failed
            if (!string.IsNullOrEmpty(savedImageUrl))
            {
                FileHelper.DeleteImage(savedImageUrl, _env);
            }

            string errorMsg = !string.IsNullOrEmpty(err) && !err.StartsWith("{")
                ? err
                : "Unable to add category. Please check the entered details and try again.";

            if (!string.IsNullOrEmpty(err) && err.Contains("already exists", StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError("CategoryName", errorMsg);
            }
            else
            {
                ModelState.AddModelError(string.Empty, errorMsg);
            }
            return View(category);
        }

        // GET: /Category/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            if (id <= 0)
            {
                return BadRequest();
            }

            int? currentUserId = Utility.Helper.GetUserId(User);
            var (categoryRes, _, _) = await _categoryService.GetCategoryById(new GetCategoryByIdReq { CategoryId = id, UserId = currentUserId });
            if (categoryRes == null)
            {
                TempData["Error"] = "Category not found.";
                return RedirectToAction(nameof(Index));
            }

            var category = new Category
            {
                CategoryId = categoryRes.CategoryId,
                CategoryName = categoryRes.CategoryName,
                ImageUrl = categoryRes.ImageUrl,
                IsActive = categoryRes.IsActive,
                UserId = categoryRes.UserId,
                CreatedDate = categoryRes.CreatedDate
            };

            return View(category);
        }

        // POST: /Category/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Category category)
        {
            if (id != category.CategoryId)
            {
                return BadRequest();
            }

            int? currentUserId = Utility.Helper.GetUserId(User);
            var (existing, _, _) = await _categoryService.GetCategoryById(new GetCategoryByIdReq { CategoryId = id, UserId = currentUserId });
            if (existing == null)
            {
                TempData["Error"] = "Category not found.";
                return RedirectToAction(nameof(Index));
            }

            // Image validation: Must either have an existing image or a newly uploaded image
            bool hasExisting = !string.IsNullOrEmpty(existing.ImageUrl);
            var (imgValid, imgErr) = FileHelper.ValidateImage(category.ImageFile, isRequired: !hasExisting);
            if (!imgValid)
            {
                ModelState.AddModelError("ImageFile", imgErr!);
            }

            if (!ModelState.IsValid)
            {
                category.ImageUrl = existing.ImageUrl;
                return View(category);
            }

            string finalImageUrl = existing.ImageUrl ?? string.Empty;
            if (category.ImageFile != null && category.ImageFile.Length > 0)
            {
                finalImageUrl = await FileHelper.SaveImageAsync(category.ImageFile, _env, "categories");
                FileHelper.DeleteImage(existing.ImageUrl, _env);
            }

            var (success, _, err) = await _categoryService.SaveCategory(new SaveCategoryReq
            {
                CategoryId = category.CategoryId,
                CategoryName = category.CategoryName,
                ImageUrl = finalImageUrl,
                IsActive = category.IsActive,
                UserId = currentUserId
            });

            if (success)
            {
                TempData["Success"] = "Category updated successfully.";
                return RedirectToAction(nameof(Index));
            }

            string errorMsg = !string.IsNullOrEmpty(err) && !err.StartsWith("{")
                ? err
                : "Unable to update category. Please check the entered details and try again.";

            if (!string.IsNullOrEmpty(err) && err.Contains("already exists", StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError("CategoryName", errorMsg);
            }
            else
            {
                ModelState.AddModelError(string.Empty, errorMsg);
            }
            category.ImageUrl = existing.ImageUrl;
            return View(category);
        }

        // GET: /Category/ListApi
        [HttpGet]
        public async Task<IActionResult> ListApi()
        {
            int? currentUserId = Utility.Helper.GetUserId(User);
            var (categoriesRes, _, _) = await _categoryService.GetCategory(new GetCategoryReq { PageSize = 100, UserId = currentUserId });
            return Json(new
            {
                success = true,
                data = categoriesRes.Select(c => new
                {
                    categoryId = c.CategoryId,
                    categoryName = c.CategoryName,
                    imageUrl = c.ImageUrl,
                    isActive = c.IsActive,
                    userId = c.UserId,
                    createdDate = c.CreatedDate.HasValue ? c.CreatedDate.Value.ToString("yyyy-MM-dd hh:mm tt") : "-"
                })
            });
        }

        // POST: /Category/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            if (id <= 0)
            {
                if (IsAjaxRequest())
                    return Json(new { success = false, message = "Invalid Category ID." });

                return BadRequest();
            }

            int? currentUserId = Utility.Helper.GetUserId(User);
            var (existing, _, _) = await _categoryService.GetCategoryById(new GetCategoryByIdReq { CategoryId = id, UserId = currentUserId });
            var (res, _, _) = await _categoryService.DeleteCategory(new DeleteReq { Id = id, UserId = currentUserId });

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

        // POST: /Category/ToggleStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            if (id <= 0)
            {
                if (IsAjaxRequest())
                    return Json(new { success = false, message = "Invalid Category ID." });

                return BadRequest();
            }

            int? currentUserId = Utility.Helper.GetUserId(User);
            var (res, _, _) = await _categoryService.ToggleStatus(new ToggleStatusReq { Id = id, UserId = currentUserId });

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
