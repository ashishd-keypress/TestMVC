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

        public Task<List<Expense>> GetExpenses(string? search = null)
        {
            return _expenseRepository.GetExpenses(search);
        }

        public Task<Expense?> GetExpenseById(long expenseId)
        {
            return _expenseRepository.GetExpenseById(expenseId);
        }

        public Task<bool> SaveExpense(Expense expense)
        {
            return _expenseRepository.SaveExpense(expense);
        }

        public Task<bool> DeleteExpense(long expenseId)
        {
            return _expenseRepository.DeleteExpense(expenseId);
        }

        public Task<List<ExpenseCategory>> GetExpenseCategories()
        {
            return _expenseRepository.GetExpenseCategories();
        }
    }
}
