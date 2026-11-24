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

        public Task<List<Expense>> GetExpenses(string? search = null, int? userId = null)
        {
            return _expenseRepository.GetExpenses(search, userId);
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
    }
}
