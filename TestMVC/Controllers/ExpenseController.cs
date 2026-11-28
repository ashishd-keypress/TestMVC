using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using TestMVC.Interface;
using TestMVC.Models;

namespace TestMVC.Controllers
{
    [Authorize]
    public class ExpenseController : Controller
    {
        private readonly IExpenseService _expenseService;
        private readonly ISalesService _salesService;

        public ExpenseController(IExpenseService expenseService, ISalesService salesService)
        {
            _expenseService = expenseService;
            _salesService = salesService;
        }

        // GET: /Expense
        [HttpGet]
        public async Task<IActionResult> Index(
            string? search, 
            string? month = null, 
            string? tab = null, 
            int? paymentMethodId = null,
            string? sortBy = null,
            string? sortOrder = null)
        {
            int? currentUserId = Utility.Helper.GetUserId(User);

            string effectiveMonth = month == null
                ? DateTime.Today.ToString("yyyy-MM")
                : month.Trim();

            int? filterYear = null;
            int? filterMonth = null;
            if (!string.IsNullOrWhiteSpace(effectiveMonth) &&
                !string.Equals(effectiveMonth, "all", StringComparison.OrdinalIgnoreCase) &&
                Utility.ExpenseStatsHelper.TryParseYearMonth(effectiveMonth, out int y, out int m))
            {
                filterYear = y;
                filterMonth = m;
            }

            var allExpenses = await _expenseService.GetExpenses(null, currentUserId);
            var paymentMethods = await _salesService.GetPaymentMethods(currentUserId);
            var expenses = (filterYear.HasValue || !string.IsNullOrWhiteSpace(search) || paymentMethodId.HasValue || !string.IsNullOrWhiteSpace(sortBy))
                ? await _expenseService.GetExpenses(search, currentUserId, filterYear, filterMonth, paymentMethodId, sortBy, sortOrder)
                : allExpenses;

            ViewBag.Search = search;
            ViewBag.SelectedMonth = filterYear.HasValue ? $"{filterYear.Value:D4}-{filterMonth!.Value:D2}" : string.Empty;
            ViewBag.ActiveTab = string.Equals(tab, "records", StringComparison.OrdinalIgnoreCase) ? "records" : "summary";
            ViewBag.AllExpenses = allExpenses;
            ViewBag.PaymentMethods = paymentMethods;
            ViewBag.SelectedPaymentMethodId = paymentMethodId;
            ViewBag.SortBy = sortBy;
            ViewBag.SortOrder = sortOrder;
            return View(expenses);
        }

        // GET: /Expense/Create
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            int? currentUserId = Utility.Helper.GetUserId(User);
            var defaultPayId = await LoadDropdownsAsync(currentUserId: currentUserId);
            return View(new Expense
            {
                ExpenseDate = System.DateTime.Today,
                PaymentMethodId = defaultPayId
            });
        }

        // POST: /Expense/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Expense expense)
        {
            int? currentUserId = Utility.Helper.GetUserId(User);
            expense.UserId = currentUserId;
            expense.CreatedBy = currentUserId;

            if (!ModelState.IsValid)
            {
                await LoadDropdownsAsync(expense.ExpenseCategoryId, expense.PaymentMethodId, currentUserId);
                return View(expense);
            }

            var success = await _expenseService.SaveExpense(expense);
            if (success)
            {
                TempData["Success"] = "Expense recorded successfully.";
                return RedirectToAction(nameof(Index));
            }

            ModelState.AddModelError(string.Empty, "Unable to record expense.");
            await LoadDropdownsAsync(expense.ExpenseCategoryId, expense.PaymentMethodId, currentUserId);
            return View(expense);
        }

        // GET: /Expense/Edit/5
        [HttpGet]
        public async Task<IActionResult> Edit(long id)
        {
            int? currentUserId = Utility.Helper.GetUserId(User);
            var expense = await _expenseService.GetExpenseById(id, currentUserId);
            if (expense == null)
            {
                TempData["Error"] = "Expense record not found.";
                return RedirectToAction(nameof(Index));
            }

            await LoadDropdownsAsync(expense.ExpenseCategoryId, expense.PaymentMethodId, currentUserId);
            return View(expense);
        }

        // POST: /Expense/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Expense expense)
        {
            int? currentUserId = Utility.Helper.GetUserId(User);
            expense.UserId = currentUserId;
            expense.CreatedBy = currentUserId;

            if (!ModelState.IsValid)
            {
                await LoadDropdownsAsync(expense.ExpenseCategoryId, expense.PaymentMethodId, currentUserId);
                return View(expense);
            }

            var success = await _expenseService.SaveExpense(expense);
            if (success)
            {
                TempData["Success"] = "Expense updated successfully.";
                return RedirectToAction(nameof(Index));
            }

            ModelState.AddModelError(string.Empty, "Unable to update expense.");
            await LoadDropdownsAsync(expense.ExpenseCategoryId, expense.PaymentMethodId, currentUserId);
            return View(expense);
        }

        // POST: /Expense/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(long id)
        {
            int? currentUserId = Utility.Helper.GetUserId(User);
            var success = await _expenseService.DeleteExpense(id, currentUserId);
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                Request.Headers.Accept.ToString().Contains("application/json"))
            {
                if (!success)
                {
                    return Json(new { success = false, message = "Failed to delete expense." });
                }

                // Recalculate fresh stats directly from DB
                var allExpenses = await _expenseService.GetExpenses(userId: currentUserId);
                return Json(new
                {
                    success = true,
                    message = "Expense deleted successfully.",
                    stats = Utility.ExpenseStatsHelper.BuildStats(allExpenses)
                });
            }

            if (success) TempData["Success"] = "Expense deleted successfully.";
            else TempData["Error"] = "Failed to delete expense.";

            return RedirectToAction(nameof(Index));
        }

        private async Task<int?> LoadDropdownsAsync(int? selectedCat = null, int? selectedPay = null, int? currentUserId = null)
        {
            currentUserId ??= Utility.Helper.GetUserId(User);
            var cats = await _expenseService.GetExpenseCategories();
            var payMethods = await _salesService.GetPaymentMethods(currentUserId);
            int? defaultPayId = selectedPay ?? payMethods.FirstOrDefault()?.PaymentMethodId;

            ViewBag.Categories = new SelectList(cats, "ExpenseCategoryId", "CategoryName", selectedCat);
            ViewBag.PaymentMethods = new SelectList(payMethods, "PaymentMethodId", "MethodName", defaultPayId);
            return defaultPayId;
        }
    }
}
