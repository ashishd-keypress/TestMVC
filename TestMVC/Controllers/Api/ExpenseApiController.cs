using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TestMVC.Interface;
using TestMVC.Models;
using Utility;

namespace TestMVC.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    public class ExpenseController : ControllerBase
    {
        private readonly IExpenseService _expenseService;

        public ExpenseController(IExpenseService expenseService)
        {
            _expenseService = expenseService;
        }

        [HttpGet]
        [Route("")]
        [Route("expense_list_get")]
        [Route("search")]
        [Route("list")]
        public async Task<IActionResult> Search([FromQuery] string? search)
        {
            int? currentUserId = Utility.Helper.GetUserId(User);
            var expenses = await _expenseService.GetExpenses(search, currentUserId);
            var allExpenses = string.IsNullOrWhiteSpace(search)
                ? expenses
                : await _expenseService.GetExpenses(null, currentUserId);

            return Ok(new
            {
                success = true,
                data = expenses.Select(e => new
                {
                    expenseId = e.ExpenseId,
                    expenseCategoryId = e.ExpenseCategoryId,
                    categoryName = e.CategoryName ?? "Other",
                    branchId = e.BranchId,
                    expenseDate = e.ExpenseDate.ToString("yyyy-MM-dd"),
                    amount = e.Amount,
                    amountFormatted = e.Amount.ToString("N2"),
                    description = e.Description,
                    paymentMethodId = e.PaymentMethodId,
                    paymentMethodName = e.PaymentMethodName,
                    userId = e.UserId,
                    createdDate = e.CreatedDate
                }),
                stats = ExpenseStatsHelper.BuildStats(allExpenses),
                totalRecords = expenses.Count,
                message = "Expenses retrieved successfully."
            });
        }

        [HttpGet]
        [Route("expense_detail_get")]
        [Route("{id:long}")]
        public async Task<IActionResult> GetById([FromQuery(Name = "id")] long? queryId = null, long? id = null)
        {
            long targetId = (queryId ?? 0) > 0 ? queryId!.Value : (id ?? 0);
            if (targetId <= 0) return BadRequest(new { success = false, message = "Invalid expense ID." });

            int? currentUserId = Utility.Helper.GetUserId(User);
            var expense = await _expenseService.GetExpenseById(targetId, currentUserId);
            if (expense == null)
            {
                return NotFound(new { success = false, message = "Expense record not found." });
            }

            return Ok(new { success = true, data = expense });
        }

        [HttpGet]
        [Route("expense_category_list_get")]
        [Route("categories")]
        public async Task<IActionResult> GetCategories()
        {
            var categories = await _expenseService.GetExpenseCategories();
            return Ok(new { success = true, data = categories });
        }

        [HttpPost]
        [Route("")]
        [Route("expense_detail_create")]
        [Route("create")]
        public async Task<IActionResult> Create([FromBody] Expense expense)
        {
            if (expense == null)
            {
                return BadRequest(new { success = false, message = "Request body is required." });
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState.ToDictionary(
                    k => k.Key,
                    v => v.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>()
                );
                return BadRequest(new { success = false, message = "Validation failed.", errors });
            }

            int? currentUserId = Utility.Helper.GetUserId(User);
            expense.ExpenseId = 0;
            expense.UserId = currentUserId;
            expense.CreatedBy = currentUserId;
            if (expense.BranchId <= 0) expense.BranchId = 1;

            var success = await _expenseService.SaveExpense(expense);
            if (!success)
            {
                return BadRequest(new { success = false, message = "Unable to record expense." });
            }

            return StatusCode(201, new
            {
                success = true,
                message = "Expense recorded successfully.",
                data = expense,
                redirectUrl = "/Expense"
            });
        }

        [HttpPut]
        [HttpPost]
        [Route("expense_detail_update")]
        [Route("{id:long}")]
        [Route("update/{id:long}")]
        [Route("update")]
        public async Task<IActionResult> Update([FromBody] Expense expense, [FromQuery(Name = "id")] long? queryId = null, long? id = null)
        {
            if (expense == null)
            {
                return BadRequest(new { success = false, message = "Request body is required." });
            }

            if ((queryId ?? 0) > 0)
            {
                expense.ExpenseId = queryId!.Value;
            }
            else if (id.HasValue && id.Value > 0)
            {
                expense.ExpenseId = id.Value;
            }

            if (expense.ExpenseId <= 0)
            {
                return BadRequest(new { success = false, message = "Valid ExpenseId is required for update." });
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState.ToDictionary(
                    k => k.Key,
                    v => v.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>()
                );
                return BadRequest(new { success = false, message = "Validation failed.", errors });
            }

            int? currentUserId = Utility.Helper.GetUserId(User);
            var existing = await _expenseService.GetExpenseById(expense.ExpenseId, currentUserId);
            if (existing == null)
            {
                return NotFound(new { success = false, message = "Expense record not found." });
            }

            expense.UserId = currentUserId;
            expense.CreatedBy = currentUserId;
            if (expense.BranchId <= 0) expense.BranchId = existing.BranchId > 0 ? existing.BranchId : 1;

            var success = await _expenseService.SaveExpense(expense);
            if (!success)
            {
                return BadRequest(new { success = false, message = "Unable to update expense." });
            }

            return Ok(new
            {
                success = true,
                message = "Expense updated successfully.",
                data = expense,
                redirectUrl = "/Expense"
            });
        }

        [HttpDelete]
        [HttpPost]
        [Route("expense_delete")]
        [Route("{id:long}")]
        [Route("delete-expense")]
        [Route("delete-expense/{id:long}")]
        [Route("Delete/{id:long}")]
        public async Task<IActionResult> Delete([FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] TestMVC.Models.ReqRes.DeleteReq? model = null, [FromQuery(Name = "id")] long? queryId = null, long id = 0)
        {
            long targetId = (queryId ?? 0) > 0 ? queryId!.Value : (id > 0 ? id : (model != null && model.Id > 0 ? model.Id : 0));
            if (targetId <= 0)
            {
                return BadRequest(new { success = false, message = "Invalid expense identifier." });
            }

            int? currentUserId = Utility.Helper.GetUserId(User);
            var success = await _expenseService.DeleteExpense(targetId, currentUserId);
            if (!success)
            {
                return NotFound(new { success = false, message = "Failed to delete expense or record not found." });
            }

            var allExpenses = await _expenseService.GetExpenses(userId: currentUserId);
            return Ok(new
            {
                success = true,
                message = "Expense deleted successfully.",
                stats = ExpenseStatsHelper.BuildStats(allExpenses)
            });
        }
    }
}
