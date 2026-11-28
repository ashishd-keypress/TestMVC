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
    [Route("api/Expense")]
    public class ExpenseApiController : ControllerBase
    {
        private readonly IExpenseService _expenseService;

        public ExpenseApiController(IExpenseService expenseService)
        {
            _expenseService = expenseService;
        }

        [HttpGet]
        [Route("expense_monthly_summary_get")]
        public async Task<IActionResult> GetMonthlySummary([FromQuery] string? month = null)
        {
            int? filterYear = null;
            int? filterMonth = null;
            if (!string.IsNullOrWhiteSpace(month) &&
                !string.Equals(month.Trim(), "all", StringComparison.OrdinalIgnoreCase))
            {
                if (!ExpenseStatsHelper.TryParseYearMonth(month, out int y, out int m))
                {
                    return BadRequest(new { success = false, message = "Invalid month format. Expected yyyy-MM." });
                }
                filterYear = y;
                filterMonth = m;
            }

            int? currentUserId = Utility.Helper.GetUserId(User);
            var allExpenses = await _expenseService.GetExpenses(null, currentUserId);
            var stats = ExpenseStatsHelper.BuildStats(allExpenses, filterYear, filterMonth);

            return Ok(new
            {
                success = true,
                selectedMonth = filterYear.HasValue ? $"{filterYear.Value:D4}-{filterMonth!.Value:D2}" : null,
                stats,
                message = "Monthly expense summary retrieved successfully."
            });
        }

        [HttpGet]
        [Route("expense_list_get")]
        public async Task<IActionResult> Search(
            [FromQuery] string? search = null, 
            [FromQuery] string? month = null, 
            [FromQuery] int? paymentMethodId = null,
            [FromQuery] string? sortBy = null,
            [FromQuery] string? sortOrder = null)
        {
            int? filterYear = null;
            int? filterMonth = null;
            if (!string.IsNullOrWhiteSpace(month) &&
                !string.Equals(month.Trim(), "all", StringComparison.OrdinalIgnoreCase))
            {
                if (!ExpenseStatsHelper.TryParseYearMonth(month, out int y, out int m))
                {
                    return BadRequest(new { success = false, message = "Invalid month format. Expected yyyy-MM." });
                }
                filterYear = y;
                filterMonth = m;
            }

            int? currentUserId = Utility.Helper.GetUserId(User);
            var expenses = await _expenseService.GetExpenses(search, currentUserId, filterYear, filterMonth, paymentMethodId, sortBy, sortOrder);
            var allExpenses = (string.IsNullOrWhiteSpace(search) && !filterYear.HasValue && !paymentMethodId.HasValue)
                ? expenses
                : await _expenseService.GetExpenses(null, currentUserId);

            return Ok(new
            {
                success = true,
                selectedMonth = filterYear.HasValue ? $"{filterYear.Value:D4}-{filterMonth!.Value:D2}" : null,
                selectedPaymentMethodId = paymentMethodId,
                sortBy = sortBy,
                sortOrder = sortOrder,
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
                stats = ExpenseStatsHelper.BuildStats(allExpenses, filterYear, filterMonth),
                totalRecords = expenses.Count,
                message = "Expenses retrieved successfully."
            });
        }

        [HttpGet]
        [Route("expense_detail_get")]
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

        [HttpPost]
        [Route("expense_detail_create")]
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
        [Route("expense_detail_update")]
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
        [Route("expense_delete")]
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
