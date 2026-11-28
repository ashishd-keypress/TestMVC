using System.Collections.Generic;
using System.Threading.Tasks;
using TestMVC.Models;

namespace TestMVC.Interface
{
    public interface IExpenseRepository
    {
        Task<List<Expense>> GetExpenses(string? search = null, int? userId = null, int? year = null, int? month = null, int? paymentMethodId = null, string? sortBy = null, string? sortOrder = null);
        Task<Expense?> GetExpenseById(long expenseId, int? userId = null);
        Task<bool> SaveExpense(Expense expense);
        Task<bool> DeleteExpense(long expenseId, int? userId = null);
        Task<List<ExpenseCategory>> GetExpenseCategories();
        Task<ExpenseCategory?> GetExpenseCategoryById(int expenseCategoryId);
        Task<(bool Success, string Message, ExpenseCategory? Category)> SaveExpenseCategory(ExpenseCategory category);
        Task<(bool Success, string Message)> DeleteExpenseCategory(int expenseCategoryId);
    }

    public interface IExpenseService
    {
        Task<List<Expense>> GetExpenses(string? search = null, int? userId = null, int? year = null, int? month = null, int? paymentMethodId = null, string? sortBy = null, string? sortOrder = null);
        Task<Expense?> GetExpenseById(long expenseId, int? userId = null);
        Task<bool> SaveExpense(Expense expense);
        Task<bool> DeleteExpense(long expenseId, int? userId = null);
        Task<List<ExpenseCategory>> GetExpenseCategories();
        Task<ExpenseCategory?> GetExpenseCategoryById(int expenseCategoryId);
        Task<(bool Success, string Message, ExpenseCategory? Category)> SaveExpenseCategory(ExpenseCategory category);
        Task<(bool Success, string Message)> DeleteExpenseCategory(int expenseCategoryId);
    }
}
