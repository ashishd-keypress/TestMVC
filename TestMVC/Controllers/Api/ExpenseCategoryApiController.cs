using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;
using TestMVC.Interface;
using TestMVC.Models;
using TestMVC.Models.ReqRes;

namespace TestMVC.Controllers.Api
{
    [ApiController]
    [Route("api/ExpenseCategory")]
    public class ExpenseCategoryApiController : ControllerBase
    {
        private readonly IExpenseService _expenseService;

        public ExpenseCategoryApiController(IExpenseService expenseService)
        {
            _expenseService = expenseService;
        }

        [HttpGet]
        [Route("expense_category_list_get")]
        public async Task<IActionResult> GetAll()
        {
            var categories = await _expenseService.GetExpenseCategories();
            return Ok(new
            {
                success = true,
                data = categories.Select(c => new
                {
                    expenseCategoryId = c.ExpenseCategoryId,
                    categoryName = c.CategoryName,
                    isActive = c.IsActive
                }),
                totalRecords = categories.Count,
                message = "Expense categories retrieved successfully."
            });
        }

        [HttpGet]
        [Route("expense_category_detail_get")]
        public async Task<IActionResult> GetById([FromQuery(Name = "id")] int? queryId = null, int id = 0)
        {
            int targetId = (queryId ?? 0) > 0 ? queryId!.Value : id;
            if (targetId <= 0)
            {
                return BadRequest(new { success = false, message = "Invalid expense category identifier." });
            }

            var category = await _expenseService.GetExpenseCategoryById(targetId);
            if (category == null)
            {
                return NotFound(new { success = false, message = "Expense category not found." });
            }

            return Ok(new
            {
                success = true,
                data = new
                {
                    expenseCategoryId = category.ExpenseCategoryId,
                    categoryName = category.CategoryName,
                    isActive = category.IsActive
                }
            });
        }

        [HttpPost]
        [Route("expense_category_create")]
        public async Task<IActionResult> Create([FromBody] ExpenseCategory? category)
        {
            if (category == null)
            {
                return BadRequest(new { success = false, message = "Request body is required." });
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState.ToDictionary(
                    k => k.Key,
                    v => v.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? System.Array.Empty<string>()
                );
                return BadRequest(new { success = false, message = "Validation failed.", errors });
            }

            category.ExpenseCategoryId = 0;
            var (success, message, saved) = await _expenseService.SaveExpenseCategory(category);
            if (!success || saved == null)
            {
                return BadRequest(new { success = false, message });
            }

            return StatusCode(201, new
            {
                success = true,
                message,
                data = new
                {
                    expenseCategoryId = saved.ExpenseCategoryId,
                    categoryName = saved.CategoryName,
                    isActive = saved.IsActive
                }
            });
        }

        [HttpPut]
        [Route("expense_category_update")]
        public async Task<IActionResult> Update([FromBody] ExpenseCategory? category, [FromQuery(Name = "id")] int? queryId = null, int id = 0)
        {
            if (category == null)
            {
                return BadRequest(new { success = false, message = "Request body is required." });
            }

            if ((queryId ?? 0) > 0)
            {
                category.ExpenseCategoryId = queryId!.Value;
            }
            else if (id > 0)
            {
                category.ExpenseCategoryId = id;
            }

            if (category.ExpenseCategoryId <= 0)
            {
                return BadRequest(new { success = false, message = "Valid ExpenseCategoryId is required for update." });
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState.ToDictionary(
                    k => k.Key,
                    v => v.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? System.Array.Empty<string>()
                );
                return BadRequest(new { success = false, message = "Validation failed.", errors });
            }

            var (success, message, saved) = await _expenseService.SaveExpenseCategory(category);
            if (!success || saved == null)
            {
                if (message.Contains("not found", System.StringComparison.OrdinalIgnoreCase))
                {
                    return NotFound(new { success = false, message });
                }
                return BadRequest(new { success = false, message });
            }

            return Ok(new
            {
                success = true,
                message,
                data = new
                {
                    expenseCategoryId = saved.ExpenseCategoryId,
                    categoryName = saved.CategoryName,
                    isActive = saved.IsActive
                }
            });
        }

        [HttpDelete]
        [Route("expense_category_delete")]
        public async Task<IActionResult> Delete([FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] DeleteReq? model = null, [FromQuery(Name = "id")] int? queryId = null, int id = 0)
        {
            int targetId = (queryId ?? 0) > 0 ? queryId!.Value : (id > 0 ? id : (model != null && model.Id > 0 ? model.Id : 0));
            if (targetId <= 0)
            {
                return BadRequest(new { success = false, message = "Invalid expense category identifier." });
            }

            var (success, message) = await _expenseService.DeleteExpenseCategory(targetId);
            if (!success)
            {
                if (message.Contains("not found", System.StringComparison.OrdinalIgnoreCase))
                {
                    return NotFound(new { success = false, message });
                }
                return BadRequest(new { success = false, message });
            }

            return Ok(new
            {
                success = true,
                message
            });
        }
    }
}
