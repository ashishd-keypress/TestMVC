using System.Collections.Generic;
using System.Threading.Tasks;
using TestMVC.Models;

namespace TestMVC.Interface
{
    public interface IExpenseRepository
    {
        Task<List<Expense>> GetExpenses(string? search = null, int? userId = null);
        Task<Expense?> GetExpenseById(long expenseId, int? userId = null);
        Task<bool> SaveExpense(Expense expense);
        Task<bool> DeleteExpense(long expenseId, int? userId = null);
        Task<List<ExpenseCategory>> GetExpenseCategories();
    }

    public interface IExpenseService
    {
        Task<List<Expense>> GetExpenses(string? search = null, int? userId = null);
        Task<Expense?> GetExpenseById(long expenseId, int? userId = null);
        Task<bool> SaveExpense(Expense expense);
        Task<bool> DeleteExpense(long expenseId, int? userId = null);
        Task<List<ExpenseCategory>> GetExpenseCategories();
    }
}
