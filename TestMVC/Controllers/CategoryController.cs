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
    public class CategoryController : Controller
    {
        private readonly ICategoryService _categoryService;

        public CategoryController(ICategoryService categoryService)
        {
            _categoryService = categoryService;
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
            if (!ModelState.IsValid)
            {
                return View(category);
            }

            int? currentUserId = Utility.Helper.GetUserId(User);
            var (success, _, err) = await _categoryService.SaveCategory(new SaveCategoryReq
            {
                CategoryId = 0,
                CategoryName = category.CategoryName,
                IsActive = category.IsActive,
                UserId = currentUserId
            });

            if (success)
            {
                TempData["Success"] = "Category added successfully.";
                return RedirectToAction(nameof(Index));
            }

            ModelState.AddModelError(string.Empty, "Failed to add category: " + err);
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

            if (!ModelState.IsValid)
            {
                return View(category);
            }

            int? currentUserId = Utility.Helper.GetUserId(User);
            var (success, _, err) = await _categoryService.SaveCategory(new SaveCategoryReq
            {
                CategoryId = category.CategoryId,
                CategoryName = category.CategoryName,
                IsActive = category.IsActive,
                UserId = currentUserId
            });

            if (success)
            {
                TempData["Success"] = "Category updated successfully.";
                return RedirectToAction(nameof(Index));
            }

            ModelState.AddModelError(string.Empty, "Failed to update category: " + err);
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
                    isActive = c.IsActive,
                    userId = c.UserId,
                    createdDate = c.CreatedDate.HasValue ? c.CreatedDate.Value.ToString("yyyy-MM-dd HH:mm") : "-"
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
            var (res, _, _) = await _categoryService.DeleteCategory(new DeleteReq { Id = id, UserId = currentUserId });

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
