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
    public class CustomerRepository : ICustomerRepository
    {
        private readonly string _connectionString;

        public CustomerRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("DefaultConnection missing");
        }

        public async Task<List<Customer>> GetCustomers(string? search = null, int? userId = null)
        {
            var list = new List<Customer>();
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var cmd = new SqlCommand(@"
                SELECT 
                    CustomerId,
                    CustomerName,
                    Phone,
                    Email,
                    GSTNumber,
                    Address,
                    OpeningBalance,
                    CurrentBalance,
                    IsActive,
                    UserId,
                    CreatedDate
                FROM dbo.Customer
                WHERE (CASE WHEN COL_LENGTH('dbo.Customer', 'IsDelete') IS NOT NULL THEN ISNULL(IsDelete, 0) ELSE 0 END) = 0
                  AND (@UserId IS NULL OR ISNULL(UserId, 1) = @UserId)
                  AND (@Search IS NULL 
                       OR CustomerName LIKE '%' + @Search + '%' 
                       OR Phone LIKE '%' + @Search + '%' 
                       OR Email LIKE '%' + @Search + '%'
                       OR GSTNumber LIKE '%' + @Search + '%')
                ORDER BY CustomerId DESC;", connection);

            cmd.Parameters.AddWithValue("@Search", (object?)search ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@UserId", (object?)userId ?? DBNull.Value);

            using var reader = await cmd.ExecuteReaderAsync();
            return reader.DataReaderMapToList<Customer>();
        }

        public async Task<Customer?> GetCustomerById(long customerId, int? userId = null)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var cmd = new SqlCommand(@"
                SELECT 
                    CustomerId,
                    CustomerName,
                    Phone,
                    Email,
                    GSTNumber,
                    Address,
                    OpeningBalance,
                    CurrentBalance,
                    IsActive,
                    UserId,
                    CreatedDate
                FROM dbo.Customer
                WHERE CustomerId = @Id
                  AND (@UserId IS NULL OR ISNULL(UserId, 1) = @UserId)
                  AND (CASE WHEN COL_LENGTH('dbo.Customer', 'IsDelete') IS NOT NULL THEN ISNULL(IsDelete, 0) ELSE 0 END) = 0;", connection);

            cmd.Parameters.AddWithValue("@Id", customerId);
            cmd.Parameters.AddWithValue("@UserId", (object?)userId ?? DBNull.Value);

            using var reader = await cmd.ExecuteReaderAsync();
            return reader.DataReaderMapToSingle<Customer>();
        }

        public async Task<Customer?> GetCustomerByPhone(string phone, int? userId = null)
        {
            if (string.IsNullOrWhiteSpace(phone)) return null;

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var cmd = new SqlCommand(@"
                SELECT TOP 1
                    CustomerId,
                    CustomerName,
                    Phone,
                    Email,
                    GSTNumber,
                    Address,
                    OpeningBalance,
                    CurrentBalance,
                    IsActive,
                    UserId,
                    CreatedDate
                FROM dbo.Customer
                WHERE Phone = @Phone
                  AND (@UserId IS NULL OR ISNULL(UserId, 1) = @UserId)
                  AND (CASE WHEN COL_LENGTH('dbo.Customer', 'IsDelete') IS NOT NULL THEN ISNULL(IsDelete, 0) ELSE 0 END) = 0;", connection);

            cmd.Parameters.AddWithValue("@Phone", phone.Trim());
            cmd.Parameters.AddWithValue("@UserId", (object?)userId ?? DBNull.Value);

            using var reader = await cmd.ExecuteReaderAsync();
            return reader.DataReaderMapToSingle<Customer>();
        }

        public async Task<long> SaveCustomer(Customer customer)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            if (customer.CustomerId <= 0)
            {
                using var cmd = new SqlCommand(@"
                    INSERT INTO dbo.Customer (CustomerName, Phone, Email, GSTNumber, Address, OpeningBalance, CurrentBalance, IsActive, IsDelete, UserId, CreatedDate)
                    VALUES (@CustomerName, @Phone, @Email, @GSTNumber, @Address, @OpeningBalance, @OpeningBalance, @IsActive, 0, @UserId, GETDATE());
                    SELECT SCOPE_IDENTITY();", connection);

                cmd.Parameters.AddWithValue("@CustomerName", customer.CustomerName);
                cmd.Parameters.AddWithValue("@Phone", customer.Phone);
                cmd.Parameters.AddWithValue("@Email", (object?)customer.Email ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@GSTNumber", (object?)customer.GSTNumber ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Address", (object?)customer.Address ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@OpeningBalance", customer.OpeningBalance);
                cmd.Parameters.AddWithValue("@IsActive", customer.IsActive ? 1 : 0);
                cmd.Parameters.AddWithValue("@UserId", (object?)customer.UserId ?? 1);

                var res = await cmd.ExecuteScalarAsync();
                return res != null ? Convert.ToInt64(res) : 0;
            }
            else
            {
                using var cmd = new SqlCommand(@"
                    UPDATE dbo.Customer
                    SET CustomerName = @CustomerName,
                        Phone = @Phone,
                        Email = @Email,
                        GSTNumber = @GSTNumber,
                        Address = @Address,
                        OpeningBalance = @OpeningBalance,
                        CurrentBalance = ISNULL(CurrentBalance, @OpeningBalance),
                        IsActive = @IsActive
                    WHERE CustomerId = @Id
                      AND (@UserId IS NULL OR ISNULL(UserId, @UserId) = @UserId);", connection);

                cmd.Parameters.AddWithValue("@Id", customer.CustomerId);
                cmd.Parameters.AddWithValue("@CustomerName", customer.CustomerName);
                cmd.Parameters.AddWithValue("@Phone", customer.Phone);
                cmd.Parameters.AddWithValue("@Email", (object?)customer.Email ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@GSTNumber", (object?)customer.GSTNumber ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Address", (object?)customer.Address ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@OpeningBalance", customer.OpeningBalance);
                cmd.Parameters.AddWithValue("@IsActive", customer.IsActive ? 1 : 0);
                cmd.Parameters.AddWithValue("@UserId", (object?)customer.UserId ?? DBNull.Value);

                await cmd.ExecuteNonQueryAsync();
                return customer.CustomerId;
            }
        }

        public async Task<bool> DeleteCustomer(long customerId, int? userId = null)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            // Check if customer has any linked transactions/records
            using var checkCmd = new SqlCommand(@"
                SELECT 
                    (SELECT COUNT(1) FROM dbo.SalesInvoice WHERE CustomerId = @Id) +
                    (SELECT COUNT(1) FROM dbo.SalesReturn WHERE CustomerId = @Id) +
                    (SELECT COUNT(1) FROM dbo.WarrantyClaim WHERE CustomerId = @Id) +
                    (SELECT COUNT(1) FROM dbo.RepairJob WHERE CustomerId = @Id);", connection);
            checkCmd.Parameters.AddWithValue("@Id", customerId);
            var res = await checkCmd.ExecuteScalarAsync();
            int relatedCount = res != null ? Convert.ToInt32(res) : 0;

            if (relatedCount > 0)
            {
                // Soft delete to protect financial/sales history integrity
                using var softCmd = new SqlCommand(@"
                    UPDATE dbo.Customer 
                    SET IsDelete = 1, IsActive = 0 
                    WHERE CustomerId = @Id
                      AND (@UserId IS NULL OR ISNULL(UserId, 1) = @UserId);", connection);
                softCmd.Parameters.AddWithValue("@Id", customerId);
                softCmd.Parameters.AddWithValue("@UserId", (object?)userId ?? DBNull.Value);
                return await softCmd.ExecuteNonQueryAsync() > 0;
            }
            else
            {
                // Hard delete when no relational references exist
                try
                {
                    using var hardCmd = new SqlCommand(@"
                        DELETE FROM dbo.Customer 
                        WHERE CustomerId = @Id
                          AND (@UserId IS NULL OR ISNULL(UserId, 1) = @UserId);", connection);
                    hardCmd.Parameters.AddWithValue("@Id", customerId);
                    hardCmd.Parameters.AddWithValue("@UserId", (object?)userId ?? DBNull.Value);
                    return await hardCmd.ExecuteNonQueryAsync() > 0;
                }
                catch
                {
                    // Fallback to soft delete if any constraint triggers
                    using var fallbackCmd = new SqlCommand(@"
                        UPDATE dbo.Customer 
                        SET IsDelete = 1, IsActive = 0 
                        WHERE CustomerId = @Id
                          AND (@UserId IS NULL OR ISNULL(UserId, 1) = @UserId);", connection);
                    fallbackCmd.Parameters.AddWithValue("@Id", customerId);
                    fallbackCmd.Parameters.AddWithValue("@UserId", (object?)userId ?? DBNull.Value);
                    return await fallbackCmd.ExecuteNonQueryAsync() > 0;
                }
            }
        }

        public async Task<bool> ToggleStatus(long customerId, int? userId = null)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var cmd = new SqlCommand(@"
                UPDATE dbo.Customer 
                SET IsActive = CASE WHEN IsActive = 1 THEN 0 ELSE 1 END 
                WHERE CustomerId = @Id
                  AND (@UserId IS NULL OR ISNULL(UserId, 1) = @UserId);

                SELECT IsActive FROM dbo.Customer WHERE CustomerId = @Id;", connection);
            cmd.Parameters.AddWithValue("@Id", customerId);
            cmd.Parameters.AddWithValue("@UserId", (object?)userId ?? DBNull.Value);
            var res = await cmd.ExecuteScalarAsync();
            return res != null && Convert.ToBoolean(res);
        }

        public async Task<int> GetCustomerCount(int? userId = null)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var cmd = new SqlCommand(@"
                SELECT COUNT(1) FROM dbo.Customer 
                WHERE (CASE WHEN COL_LENGTH('dbo.Customer', 'IsDelete') IS NOT NULL THEN ISNULL(IsDelete, 0) ELSE 0 END) = 0
                  AND (@UserId IS NULL OR ISNULL(UserId, 1) = @UserId);", connection);
            cmd.Parameters.AddWithValue("@UserId", (object?)userId ?? DBNull.Value);
            var res = await cmd.ExecuteScalarAsync();
            return res != null ? Convert.ToInt32(res) : 0;
        }
    }
}
