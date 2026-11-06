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
    public class SupplierRepository : ISupplierRepository
    {
        private readonly string _connectionString;

        public SupplierRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("DefaultConnection missing");
        }

        public async Task<List<Supplier>> GetSuppliers(string? search = null)
        {
            var list = new List<Supplier>();
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var cmd = new SqlCommand(@"
                SELECT 
                    SupplierId,
                    SupplierName,
                    ContactPerson,
                    Phone,
                    Email,
                    GSTNumber,
                    Address,
                    OpeningBalance,
                    CurrentBalance,
                    IsActive,
                    CreatedDate
                FROM dbo.Supplier
                WHERE (CASE WHEN COL_LENGTH('dbo.Supplier', 'IsDelete') IS NOT NULL THEN ISNULL(IsDelete, 0) ELSE 0 END) = 0
                  AND (@Search IS NULL 
                       OR SupplierName LIKE '%' + @Search + '%' 
                       OR Phone LIKE '%' + @Search + '%' 
                       OR ContactPerson LIKE '%' + @Search + '%'
                       OR GSTNumber LIKE '%' + @Search + '%')
                ORDER BY SupplierId DESC;", connection);

            cmd.Parameters.AddWithValue("@Search", (object?)search ?? DBNull.Value);

            using var reader = await cmd.ExecuteReaderAsync();
            return reader.DataReaderMapToList<Supplier>();
        }

        public async Task<Supplier?> GetSupplierById(long supplierId)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var cmd = new SqlCommand(@"
                SELECT 
                    SupplierId,
                    SupplierName,
                    ContactPerson,
                    Phone,
                    Email,
                    GSTNumber,
                    Address,
                    OpeningBalance,
                    CurrentBalance,
                    IsActive,
                    CreatedDate
                FROM dbo.Supplier
                WHERE SupplierId = @Id
                  AND (CASE WHEN COL_LENGTH('dbo.Supplier', 'IsDelete') IS NOT NULL THEN ISNULL(IsDelete, 0) ELSE 0 END) = 0;", connection);

            cmd.Parameters.AddWithValue("@Id", supplierId);

            using var reader = await cmd.ExecuteReaderAsync();
            return reader.DataReaderMapToSingle<Supplier>();
        }

        public async Task<bool> SaveSupplier(Supplier supplier)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            if (supplier.SupplierId <= 0)
            {
                using var cmd = new SqlCommand(@"
                    INSERT INTO dbo.Supplier (SupplierName, ContactPerson, Phone, Email, GSTNumber, Address, OpeningBalance, CurrentBalance, IsActive, IsDelete, CreatedDate)
                    VALUES (@SupplierName, @ContactPerson, @Phone, @Email, @GSTNumber, @Address, @OpeningBalance, @OpeningBalance, @IsActive, 0, GETDATE());
                    SELECT SCOPE_IDENTITY();", connection);

                cmd.Parameters.AddWithValue("@SupplierName", supplier.SupplierName);
                cmd.Parameters.AddWithValue("@ContactPerson", (object?)supplier.ContactPerson ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Phone", supplier.Phone);
                cmd.Parameters.AddWithValue("@Email", (object?)supplier.Email ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@GSTNumber", (object?)supplier.GSTNumber ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Address", (object?)supplier.Address ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@OpeningBalance", supplier.OpeningBalance);
                cmd.Parameters.AddWithValue("@IsActive", supplier.IsActive ? 1 : 0);

                var res = await cmd.ExecuteScalarAsync();
                return res != null && Convert.ToInt64(res) > 0;
            }
            else
            {
                using var cmd = new SqlCommand(@"
                    UPDATE dbo.Supplier
                    SET SupplierName = @SupplierName,
                        ContactPerson = @ContactPerson,
                        Phone = @Phone,
                        Email = @Email,
                        GSTNumber = @GSTNumber,
                        Address = @Address,
                        OpeningBalance = @OpeningBalance,
                        CurrentBalance = ISNULL(CurrentBalance, @OpeningBalance),
                        IsActive = @IsActive
                    WHERE SupplierId = @Id;", connection);

                cmd.Parameters.AddWithValue("@Id", supplier.SupplierId);
                cmd.Parameters.AddWithValue("@SupplierName", supplier.SupplierName);
                cmd.Parameters.AddWithValue("@ContactPerson", (object?)supplier.ContactPerson ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Phone", supplier.Phone);
                cmd.Parameters.AddWithValue("@Email", (object?)supplier.Email ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@GSTNumber", (object?)supplier.GSTNumber ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Address", (object?)supplier.Address ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@OpeningBalance", supplier.OpeningBalance);
                cmd.Parameters.AddWithValue("@IsActive", supplier.IsActive ? 1 : 0);

                int affected = await cmd.ExecuteNonQueryAsync();
                return affected > 0;
            }
        }

        public async Task<bool> DeleteSupplier(long supplierId)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            // Check if supplier has linked purchases
            using var checkCmd = new SqlCommand(@"
                SELECT COUNT(1) FROM dbo.Purchase WHERE SupplierId = @Id;", connection);
            checkCmd.Parameters.AddWithValue("@Id", supplierId);
            var res = await checkCmd.ExecuteScalarAsync();
            int purchaseCount = res != null ? Convert.ToInt32(res) : 0;

            if (purchaseCount > 0)
            {
                // Soft delete to protect purchase and inventory history
                using var softCmd = new SqlCommand(@"
                    UPDATE dbo.Supplier 
                    SET IsDelete = 1, IsActive = 0 
                    WHERE SupplierId = @Id;", connection);
                softCmd.Parameters.AddWithValue("@Id", supplierId);
                return await softCmd.ExecuteNonQueryAsync() > 0;
            }
            else
            {
                // Hard delete when no relational references exist
                try
                {
                    using var hardCmd = new SqlCommand(@"
                        DELETE FROM dbo.Supplier 
                        WHERE SupplierId = @Id;", connection);
                    hardCmd.Parameters.AddWithValue("@Id", supplierId);
                    return await hardCmd.ExecuteNonQueryAsync() > 0;
                }
                catch
                {
                    // Fallback to soft delete
                    using var fallbackCmd = new SqlCommand(@"
                        UPDATE dbo.Supplier 
                        SET IsDelete = 1, IsActive = 0 
                        WHERE SupplierId = @Id;", connection);
                    fallbackCmd.Parameters.AddWithValue("@Id", supplierId);
                    return await fallbackCmd.ExecuteNonQueryAsync() > 0;
                }
            }
        }

        public async Task<bool> ToggleStatus(long supplierId)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var cmd = new SqlCommand(@"
                UPDATE dbo.Supplier 
                SET IsActive = CASE WHEN IsActive = 1 THEN 0 ELSE 1 END 
                WHERE SupplierId = @Id;

                SELECT IsActive FROM dbo.Supplier WHERE SupplierId = @Id;", connection);
            cmd.Parameters.AddWithValue("@Id", supplierId);
            var res = await cmd.ExecuteScalarAsync();
            return res != null && Convert.ToBoolean(res);
        }

        public async Task<int> GetSupplierCount()
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var cmd = new SqlCommand(@"
                SELECT COUNT(1) FROM dbo.Supplier 
                WHERE (CASE WHEN COL_LENGTH('dbo.Supplier', 'IsDelete') IS NOT NULL THEN ISNULL(IsDelete, 0) ELSE 0 END) = 0;", connection);
            var res = await cmd.ExecuteScalarAsync();
            return res != null ? Convert.ToInt32(res) : 0;
        }
    }
}
