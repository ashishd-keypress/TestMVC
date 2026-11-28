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

        public async Task<List<Expense>> GetExpenses(
            string? search = null, 
            int? userId = null, 
            int? year = null, 
            int? month = null, 
            int? paymentMethodId = null, 
            string? sortBy = null, 
            string? sortOrder = null)
        {
            var list = new List<Expense>();
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            string orderByClause = (sortBy?.Trim().ToLowerInvariant()) switch
            {
                "amount" => string.Equals(sortOrder?.Trim(), "asc", StringComparison.OrdinalIgnoreCase)
                    ? "ORDER BY e.Amount ASC, e.ExpenseDate ASC, e.ExpenseId ASC"
                    : "ORDER BY e.Amount DESC, e.ExpenseDate DESC, e.ExpenseId DESC",
                "date" => string.Equals(sortOrder?.Trim(), "asc", StringComparison.OrdinalIgnoreCase)
                    ? "ORDER BY e.ExpenseDate ASC, e.ExpenseId ASC"
                    : "ORDER BY e.ExpenseDate DESC, e.ExpenseId DESC",
                _ => "ORDER BY e.ExpenseDate DESC, e.ExpenseId DESC"
            };

            string query = $@"
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
                    e.UserId,
                    e.CreatedBy,
                    e.CreatedDate
                FROM dbo.Expense e
                LEFT JOIN dbo.ExpenseCategory ec ON e.ExpenseCategoryId = ec.ExpenseCategoryId
                LEFT JOIN dbo.PaymentMethod pm ON e.PaymentMethodId = pm.PaymentMethodId
                WHERE (@UserId IS NULL OR ISNULL(e.UserId, ISNULL(e.CreatedBy, 1)) = @UserId)
                  AND (@Year IS NULL OR YEAR(e.ExpenseDate) = @Year)
                  AND (@Month IS NULL OR MONTH(e.ExpenseDate) = @Month)
                  AND (@PaymentMethodId IS NULL OR e.PaymentMethodId = @PaymentMethodId)
                  AND (@Search IS NULL 
                       OR e.Description LIKE '%' + @Search + '%' 
                       OR ec.CategoryName LIKE '%' + @Search + '%')
                {orderByClause};";

            using var cmd = new SqlCommand(query, connection);

            cmd.Parameters.AddWithValue("@Search", (object?)search ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@UserId", (object?)userId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Year", (object?)year ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Month", (object?)month ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@PaymentMethodId", (object?)paymentMethodId ?? DBNull.Value);

            using var reader = await cmd.ExecuteReaderAsync();
            return reader.DataReaderMapToList<Expense>();
        }

        public async Task<Expense?> GetExpenseById(long expenseId, int? userId = null)
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
                    e.UserId,
                    e.CreatedBy,
                    e.CreatedDate
                FROM dbo.Expense e
                LEFT JOIN dbo.ExpenseCategory ec ON e.ExpenseCategoryId = ec.ExpenseCategoryId
                LEFT JOIN dbo.PaymentMethod pm ON e.PaymentMethodId = pm.PaymentMethodId
                WHERE e.ExpenseId = @Id
                  AND (@UserId IS NULL OR ISNULL(e.UserId, ISNULL(e.CreatedBy, 1)) = @UserId);", connection);

            cmd.Parameters.AddWithValue("@Id", expenseId);
            cmd.Parameters.AddWithValue("@UserId", (object?)userId ?? DBNull.Value);

            using var reader = await cmd.ExecuteReaderAsync();
            return reader.DataReaderMapToSingle<Expense>();
        }

        public async Task<bool> SaveExpense(Expense expense)
        {
            int effectiveUserId = expense.UserId ?? expense.CreatedBy ?? 1;
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            // Resolve user's own PaymentMethodId cleanly (never leak another user's PaymentMethodId = 1)
            int resolvedPaymentMethodId = expense.PaymentMethodId ?? 0;
            bool ownsProvidedPm = false;
            if (resolvedPaymentMethodId > 0)
            {
                using var pmCheckCmd = new SqlCommand(@"
                    SELECT COUNT(1)
                    FROM dbo.PaymentMethod
                    WHERE PaymentMethodId = @PmId
                      AND ISNULL(IsDelete, 0) = 0
                      AND ISNULL(UserId, 1) = @UserId;", connection);
                pmCheckCmd.Parameters.AddWithValue("@PmId", resolvedPaymentMethodId);
                pmCheckCmd.Parameters.AddWithValue("@UserId", effectiveUserId);
                ownsProvidedPm = Convert.ToInt32(await pmCheckCmd.ExecuteScalarAsync()) > 0;
            }

            if (!ownsProvidedPm)
            {
                // If caller explicitly passed another user's non-default ID or 0, try resolving first active user method only when <= 0
                if (resolvedPaymentMethodId > 0)
                {
                    return false;
                }

                using var pmFallbackCmd = new SqlCommand(@"
                    SELECT TOP 1 PaymentMethodId
                    FROM dbo.PaymentMethod
                    WHERE ISNULL(IsDelete, 0) = 0
                      AND IsActive = 1
                      AND ISNULL(UserId, 1) = @UserId
                    ORDER BY PaymentMethodId ASC;", connection);
                pmFallbackCmd.Parameters.AddWithValue("@UserId", effectiveUserId);
                var pmObj = await pmFallbackCmd.ExecuteScalarAsync();
                if (pmObj == null || pmObj == DBNull.Value)
                {
                    return false;
                }
                resolvedPaymentMethodId = Convert.ToInt32(pmObj);
            }

            if (expense.ExpenseId <= 0)
            {
                using var cmd = new SqlCommand(@"
                    INSERT INTO dbo.Expense (ExpenseCategoryId, BranchId, ExpenseDate, Amount, Description, PaymentMethodId, UserId, CreatedBy, CreatedDate)
                    VALUES (@ExpenseCategoryId, @BranchId, @ExpenseDate, @Amount, @Description, @PaymentMethodId, @UserId, @UserId, GETDATE());
                    SELECT SCOPE_IDENTITY();", connection);

                cmd.Parameters.AddWithValue("@ExpenseCategoryId", expense.ExpenseCategoryId);
                cmd.Parameters.AddWithValue("@BranchId", expense.BranchId > 0 ? expense.BranchId : 1);
                cmd.Parameters.AddWithValue("@ExpenseDate", expense.ExpenseDate);
                cmd.Parameters.AddWithValue("@Amount", expense.Amount);
                cmd.Parameters.AddWithValue("@Description", expense.Description);
                cmd.Parameters.AddWithValue("@PaymentMethodId", resolvedPaymentMethodId);
                cmd.Parameters.AddWithValue("@UserId", effectiveUserId);

                var res = await cmd.ExecuteScalarAsync();
                if (res != null && Convert.ToInt64(res) > 0)
                {
                    expense.ExpenseId = Convert.ToInt64(res);
                    return true;
                }
                return false;
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
                    WHERE ExpenseId = @Id
                      AND (@UserId IS NULL OR ISNULL(UserId, @UserId) = @UserId);", connection);

                cmd.Parameters.AddWithValue("@Id", expense.ExpenseId);
                cmd.Parameters.AddWithValue("@ExpenseCategoryId", expense.ExpenseCategoryId);
                cmd.Parameters.AddWithValue("@ExpenseDate", expense.ExpenseDate);
                cmd.Parameters.AddWithValue("@Amount", expense.Amount);
                cmd.Parameters.AddWithValue("@Description", expense.Description);
                cmd.Parameters.AddWithValue("@PaymentMethodId", resolvedPaymentMethodId);
                cmd.Parameters.AddWithValue("@UserId", (object?)expense.UserId ?? DBNull.Value);

                int affected = await cmd.ExecuteNonQueryAsync();
                return affected > 0;
            }
        }

        public async Task<bool> DeleteExpense(long expenseId, int? userId = null)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var cmd = new SqlCommand(@"
                DELETE FROM dbo.Expense 
                WHERE ExpenseId = @Id
                  AND (@UserId IS NULL OR ISNULL(UserId, ISNULL(CreatedBy, 1)) = @UserId);", connection);
            cmd.Parameters.AddWithValue("@Id", expenseId);
            cmd.Parameters.AddWithValue("@UserId", (object?)userId ?? DBNull.Value);
            return await cmd.ExecuteNonQueryAsync() > 0;
        }

        public async Task<List<ExpenseCategory>> GetExpenseCategories()
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var cmd = new SqlCommand(@"SELECT ExpenseCategoryId, CategoryName, IsActive FROM dbo.ExpenseCategory ORDER BY CategoryName ASC;", connection);
            using var reader = await cmd.ExecuteReaderAsync();
            return reader.DataReaderMapToList<ExpenseCategory>();
        }

        public async Task<ExpenseCategory?> GetExpenseCategoryById(int expenseCategoryId)
        {
            if (expenseCategoryId <= 0) return null;

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var cmd = new SqlCommand(@"SELECT ExpenseCategoryId, CategoryName, IsActive FROM dbo.ExpenseCategory WHERE ExpenseCategoryId = @Id;", connection);
            cmd.Parameters.AddWithValue("@Id", expenseCategoryId);
            using var reader = await cmd.ExecuteReaderAsync();
            return reader.DataReaderMapToSingle<ExpenseCategory>();
        }

        public async Task<(bool Success, string Message, ExpenseCategory? Category)> SaveExpenseCategory(ExpenseCategory category)
        {
            string trimmedName = (category.CategoryName ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(trimmedName))
            {
                return (false, "Category Name is required.", null);
            }
            if (trimmedName.Length > 100)
            {
                return (false, "Category Name cannot exceed 100 characters.", null);
            }

            category.CategoryName = trimmedName;

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using (var dupCmd = new SqlCommand(@"
                SELECT COUNT(1)
                FROM dbo.ExpenseCategory
                WHERE LOWER(LTRIM(RTRIM(CategoryName))) = LOWER(@Name)
                  AND ExpenseCategoryId <> @Id;", connection))
            {
                dupCmd.Parameters.AddWithValue("@Name", trimmedName);
                dupCmd.Parameters.AddWithValue("@Id", category.ExpenseCategoryId);
                int dupCount = Convert.ToInt32(await dupCmd.ExecuteScalarAsync());
                if (dupCount > 0)
                {
                    return (false, $"Expense category '{trimmedName}' already exists.", null);
                }
            }

            if (category.ExpenseCategoryId <= 0)
            {
                using var insertCmd = new SqlCommand(@"
                    INSERT INTO dbo.ExpenseCategory (CategoryName, IsActive)
                    VALUES (@CategoryName, @IsActive);
                    SELECT CAST(SCOPE_IDENTITY() AS INT);", connection);
                insertCmd.Parameters.AddWithValue("@CategoryName", trimmedName);
                insertCmd.Parameters.AddWithValue("@IsActive", category.IsActive ? 1 : 0);

                var newIdObj = await insertCmd.ExecuteScalarAsync();
                int newId = newIdObj != null ? Convert.ToInt32(newIdObj) : 0;
                if (newId <= 0)
                {
                    return (false, "Unable to add expense category.", null);
                }

                category.ExpenseCategoryId = newId;
                return (true, "Expense category added successfully.", category);
            }
            else
            {
                using var updateCmd = new SqlCommand(@"
                    UPDATE dbo.ExpenseCategory
                    SET CategoryName = @CategoryName,
                        IsActive = @IsActive
                    WHERE ExpenseCategoryId = @Id;", connection);
                updateCmd.Parameters.AddWithValue("@Id", category.ExpenseCategoryId);
                updateCmd.Parameters.AddWithValue("@CategoryName", trimmedName);
                updateCmd.Parameters.AddWithValue("@IsActive", category.IsActive ? 1 : 0);

                int affected = await updateCmd.ExecuteNonQueryAsync();
                if (affected <= 0)
                {
                    return (false, "Expense category not found.", null);
                }

                return (true, "Expense category updated successfully.", category);
            }
        }

        public async Task<(bool Success, string Message)> DeleteExpenseCategory(int expenseCategoryId)
        {
            if (expenseCategoryId <= 0)
            {
                return (false, "Invalid expense category ID.");
            }

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using (var existsCmd = new SqlCommand(@"SELECT COUNT(1) FROM dbo.ExpenseCategory WHERE ExpenseCategoryId = @Id;", connection))
            {
                existsCmd.Parameters.AddWithValue("@Id", expenseCategoryId);
                int exists = Convert.ToInt32(await existsCmd.ExecuteScalarAsync());
                if (exists == 0)
                {
                    return (false, "Expense category not found.");
                }
            }

            using (var refCmd = new SqlCommand(@"SELECT COUNT(1) FROM dbo.Expense WHERE ExpenseCategoryId = @Id;", connection))
            {
                refCmd.Parameters.AddWithValue("@Id", expenseCategoryId);
                int usedCount = Convert.ToInt32(await refCmd.ExecuteScalarAsync());
                if (usedCount > 0)
                {
                    return (false, $"Cannot delete this expense category because it is used by {usedCount} existing expense record(s).");
                }
            }

            using var deleteCmd = new SqlCommand(@"DELETE FROM dbo.ExpenseCategory WHERE ExpenseCategoryId = @Id;", connection);
            deleteCmd.Parameters.AddWithValue("@Id", expenseCategoryId);
            int affected = await deleteCmd.ExecuteNonQueryAsync();
            return affected > 0
                ? (true, "Expense category deleted successfully.")
                : (false, "Unable to delete expense category.");
        }
    }
}
