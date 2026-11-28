using System.Collections.Generic;
using System.Threading.Tasks;
using TestMVC.Interface;
using TestMVC.Models;
namespace TestMVC.Service
{
    public class ExpenseService : IExpenseService
    {
        private readonly IExpenseRepository _expenseRepository;

        public ExpenseService(IExpenseRepository expenseRepository)
        {
            _expenseRepository = expenseRepository;
        }

        public Task<List<Expense>> GetExpenses(string? search = null, int? userId = null, int? year = null, int? month = null, int? paymentMethodId = null, string? sortBy = null, string? sortOrder = null)
        {
            return _expenseRepository.GetExpenses(search, userId, year, month, paymentMethodId, sortBy, sortOrder);
        }

        public Task<Expense?> GetExpenseById(long expenseId, int? userId = null)
        {
            return _expenseRepository.GetExpenseById(expenseId, userId);
        }

        public Task<bool> SaveExpense(Expense expense)
        {
            return _expenseRepository.SaveExpense(expense);
        }

        public Task<bool> DeleteExpense(long expenseId, int? userId = null)
        {
            return _expenseRepository.DeleteExpense(expenseId, userId);
        }

        public Task<List<ExpenseCategory>> GetExpenseCategories()
        {
            return _expenseRepository.GetExpenseCategories();
        }

        public Task<ExpenseCategory?> GetExpenseCategoryById(int expenseCategoryId)
        {
            return _expenseRepository.GetExpenseCategoryById(expenseCategoryId);
        }

        public Task<(bool Success, string Message, ExpenseCategory? Category)> SaveExpenseCategory(ExpenseCategory category)
        {
            return _expenseRepository.SaveExpenseCategory(category);
        }

        public Task<(bool Success, string Message)> DeleteExpenseCategory(int expenseCategoryId)
        {
            return _expenseRepository.DeleteExpenseCategory(expenseCategoryId);
        }
    }
}
