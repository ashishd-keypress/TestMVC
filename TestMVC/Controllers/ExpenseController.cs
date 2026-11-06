using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Threading.Tasks;
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
            var expenses = await _expenseService.GetExpenses(search);
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

        // POST: /Expense/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(long id)
        {
            var success = await _expenseService.DeleteExpense(id);
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                Request.Headers.Accept.ToString().Contains("application/json"))
            {
                return Json(new { success, message = success ? "Expense deleted." : "Failed to delete expense." });
            }

            if (success) TempData["Success"] = "Expense deleted.";
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
