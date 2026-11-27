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
        public async Task<IActionResult> Index(string? search)
        {
            int? currentUserId = Utility.Helper.GetUserId(User);
            var expenses = await _expenseService.GetExpenses(search, currentUserId);
            ViewBag.Search = search;
            return View(expenses);
        }

        // GET: /Expense/Create
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadDropdownsAsync();
            return View(new Expense { ExpenseDate = System.DateTime.Today });
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
                await LoadDropdownsAsync(expense.ExpenseCategoryId, expense.PaymentMethodId);
                return View(expense);
            }

            var success = await _expenseService.SaveExpense(expense);
            if (success)
            {
                TempData["Success"] = "Expense recorded successfully.";
                return RedirectToAction(nameof(Index));
            }

            ModelState.AddModelError(string.Empty, "Unable to record expense.");
            await LoadDropdownsAsync(expense.ExpenseCategoryId, expense.PaymentMethodId);
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

            await LoadDropdownsAsync(expense.ExpenseCategoryId, expense.PaymentMethodId);
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
                await LoadDropdownsAsync(expense.ExpenseCategoryId, expense.PaymentMethodId);
                return View(expense);
            }

            var success = await _expenseService.SaveExpense(expense);
            if (success)
            {
                TempData["Success"] = "Expense updated successfully.";
                return RedirectToAction(nameof(Index));
            }

            ModelState.AddModelError(string.Empty, "Unable to update expense.");
            await LoadDropdownsAsync(expense.ExpenseCategoryId, expense.PaymentMethodId);
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

        private async Task LoadDropdownsAsync(int? selectedCat = null, int? selectedPay = null)
        {
            var cats = await _expenseService.GetExpenseCategories();
            var payMethods = await _salesService.GetPaymentMethods();

            ViewBag.Categories = new SelectList(cats, "ExpenseCategoryId", "CategoryName", selectedCat);
            ViewBag.PaymentMethods = new SelectList(payMethods, "PaymentMethodId", "MethodName", selectedPay ?? 1);
        }
    }
}
