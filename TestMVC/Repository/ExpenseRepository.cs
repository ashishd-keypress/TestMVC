using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using TestMVC.Interface;
using TestMVC.Models;
using Utility;

namespace TestMVC.Repository
{
    public class ExpenseRepository : IExpenseRepository
    {
        private readonly string _connectionString;

        public ExpenseRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("DefaultConnection missing");
        }

        public async Task<List<Expense>> GetExpenses(string? search = null)
        {
            var list = new List<Expense>();
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var cmd = new SqlCommand(@"
                SELECT 
                    e.ExpenseId,
                    e.ExpenseCategoryId,
                    ec.CategoryName,
                    e.BranchId,
                    e.ExpenseDate,
                    e.Amount,
                    e.Description,
                    e.PaymentMethodId,
                    pm.MethodName AS PaymentMethodName,
                    e.CreatedDate
                FROM dbo.Expense e
                LEFT JOIN dbo.ExpenseCategory ec ON e.ExpenseCategoryId = ec.ExpenseCategoryId
                LEFT JOIN dbo.PaymentMethod pm ON e.PaymentMethodId = pm.PaymentMethodId
                WHERE (@Search IS NULL 
                       OR e.Description LIKE '%' + @Search + '%' 
                       OR ec.CategoryName LIKE '%' + @Search + '%')
                ORDER BY e.ExpenseDate DESC, e.ExpenseId DESC;", connection);

            cmd.Parameters.AddWithValue("@Search", (object?)search ?? DBNull.Value);

            using var reader = await cmd.ExecuteReaderAsync();
            return reader.DataReaderMapToList<Expense>();
        }

        public async Task<Expense?> GetExpenseById(long expenseId)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var cmd = new SqlCommand(@"
                SELECT 
                    e.ExpenseId,
                    e.ExpenseCategoryId,
                    ec.CategoryName,
                    e.BranchId,
                    e.ExpenseDate,
                    e.Amount,
                    e.Description,
                    e.PaymentMethodId,
                    pm.MethodName AS PaymentMethodName,
                    e.CreatedDate
                FROM dbo.Expense e
                LEFT JOIN dbo.ExpenseCategory ec ON e.ExpenseCategoryId = ec.ExpenseCategoryId
                LEFT JOIN dbo.PaymentMethod pm ON e.PaymentMethodId = pm.PaymentMethodId
                WHERE e.ExpenseId = @Id;", connection);

            cmd.Parameters.AddWithValue("@Id", expenseId);

            using var reader = await cmd.ExecuteReaderAsync();
            return reader.DataReaderMapToSingle<Expense>();
        }

        public async Task<bool> SaveExpense(Expense expense)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            if (expense.ExpenseId <= 0)
            {
                using var cmd = new SqlCommand(@"
                    INSERT INTO dbo.Expense (ExpenseCategoryId, BranchId, ExpenseDate, Amount, Description, PaymentMethodId, CreatedDate)
                    VALUES (@ExpenseCategoryId, @BranchId, @ExpenseDate, @Amount, @Description, @PaymentMethodId, GETDATE());
                    SELECT SCOPE_IDENTITY();", connection);

                cmd.Parameters.AddWithValue("@ExpenseCategoryId", expense.ExpenseCategoryId);
                cmd.Parameters.AddWithValue("@BranchId", expense.BranchId > 0 ? expense.BranchId : 1);
                cmd.Parameters.AddWithValue("@ExpenseDate", expense.ExpenseDate);
                cmd.Parameters.AddWithValue("@Amount", expense.Amount);
                cmd.Parameters.AddWithValue("@Description", expense.Description);
                cmd.Parameters.AddWithValue("@PaymentMethodId", (object?)expense.PaymentMethodId ?? 1);

                var res = await cmd.ExecuteScalarAsync();
                return res != null && Convert.ToInt64(res) > 0;
            }
            else
            {
                using var cmd = new SqlCommand(@"
                    UPDATE dbo.Expense
                    SET ExpenseCategoryId = @ExpenseCategoryId,
                        ExpenseDate = @ExpenseDate,
                        Amount = @Amount,
                        Description = @Description,
                        PaymentMethodId = @PaymentMethodId
                    WHERE ExpenseId = @Id;", connection);

                cmd.Parameters.AddWithValue("@Id", expense.ExpenseId);
                cmd.Parameters.AddWithValue("@ExpenseCategoryId", expense.ExpenseCategoryId);
                cmd.Parameters.AddWithValue("@ExpenseDate", expense.ExpenseDate);
                cmd.Parameters.AddWithValue("@Amount", expense.Amount);
                cmd.Parameters.AddWithValue("@Description", expense.Description);
                cmd.Parameters.AddWithValue("@PaymentMethodId", (object?)expense.PaymentMethodId ?? 1);

                int affected = await cmd.ExecuteNonQueryAsync();
                return affected > 0;
            }
        }

        public async Task<bool> DeleteExpense(long expenseId)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var cmd = new SqlCommand(@"DELETE FROM dbo.Expense WHERE ExpenseId = @Id;", connection);
            cmd.Parameters.AddWithValue("@Id", expenseId);
            return await cmd.ExecuteNonQueryAsync() > 0;
        }

        public async Task<List<ExpenseCategory>> GetExpenseCategories()
        {
            var list = new List<ExpenseCategory>();
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var cmd = new SqlCommand(@"SELECT ExpenseCategoryId, CategoryName FROM dbo.ExpenseCategory ORDER BY CategoryName ASC;", connection);
            using var reader = await cmd.ExecuteReaderAsync();
            return reader.DataReaderMapToList<ExpenseCategory>();
        }
    }
}
