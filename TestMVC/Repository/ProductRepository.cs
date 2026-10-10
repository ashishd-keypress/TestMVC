using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using TestMVC.Interface;
using TestMVC.Models.ReqRes;
using Utility;

namespace TestMVC.Repository
{
    public class ProductRepository : IProductRepository
    {
        private readonly string _connectionString;

        public ProductRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("DefaultConnection is missing in appsettings.json");

            DbInitializer.EnsureDatabaseSetup(_connectionString);
        }

        public async Task<Tuple<List<GetProductRes>, int, string>> GetProduct(GetProductReq model)
        {
            string strDBError = string.Empty;
            List<GetProductRes> response = new List<GetProductRes>();
            int TotalRecords = 0;

            using (var connection = new SqlConnection(_connectionString))
            {
                try
                {
                    await connection.OpenAsync();

                    // Count total matching records
                    using (var countCmd = new SqlCommand(@"
                        SELECT COUNT(1)
                        FROM dbo.Product p
                        LEFT JOIN dbo.Category c ON p.CategoryId = c.CategoryId
                        LEFT JOIN dbo.Brand b ON p.BrandId = b.BrandId
                        WHERE (CASE WHEN COL_LENGTH('dbo.Product', 'IsDelete') IS NOT NULL THEN ISNULL(p.IsDelete, 0) ELSE 0 END) = 0
                          AND (@BrandId IS NULL OR p.BrandId = @BrandId)
                          AND (@CategoryId IS NULL OR p.CategoryId = @CategoryId)
                          AND (@IsActive IS NULL OR (CASE WHEN COL_LENGTH('dbo.Product', 'IsActive') IS NOT NULL THEN ISNULL(p.IsActive, 1) ELSE 1 END) = @IsActive)
                          AND (@UserId IS NULL OR (CASE WHEN COL_LENGTH('dbo.Product', 'UserId') IS NOT NULL THEN ISNULL(p.UserId, ISNULL(p.CreatedBY, 1)) ELSE ISNULL(p.CreatedBY, 1) END) = @UserId)
                          AND (
                                @SearchText IS NULL 
                                OR p.[Name] LIKE '%' + @SearchText + '%' 
                                OR b.BrandName LIKE '%' + @SearchText + '%' 
                                OR c.CategoryName LIKE '%' + @SearchText + '%'
                          );", connection))
                    {
                        countCmd.Parameters.AddWithValue("@SearchText", (object?)model.SearchText ?? DBNull.Value);
                        countCmd.Parameters.AddWithValue("@BrandId", (object?)model.BrandId ?? DBNull.Value);
                        countCmd.Parameters.AddWithValue("@CategoryId", (object?)model.CategoryId ?? DBNull.Value);
                        countCmd.Parameters.AddWithValue("@IsActive", (object?)model.IsActive ?? DBNull.Value);
                        countCmd.Parameters.AddWithValue("@UserId", (object?)model.UserId ?? DBNull.Value);

                        TotalRecords = Convert.ToInt32(await countCmd.ExecuteScalarAsync() ?? 0);
                    }

                    // Query page records
                    int pageNo = model.PageNo > 0 ? model.PageNo : 1;
                    int pageSize = model.PageSize > 0 ? model.PageSize : 10;
                    int offset = (pageNo - 1) * pageSize;

                    using (var cmd = new SqlCommand(@"
                        SELECT 
                            p.ProductId,
                            p.[Name],
                            p.Price,
                            p.Size,
                            p.BrandId,
                            b.BrandName AS Brand,
                            p.CategoryId,
                            c.CategoryName AS Category,
                            (CASE WHEN COL_LENGTH('dbo.Product', 'IsActive') IS NOT NULL THEN ISNULL(p.IsActive, 1) ELSE 1 END) AS IsActive,
                            (CASE WHEN COL_LENGTH('dbo.Product', 'UserId') IS NOT NULL THEN p.UserId ELSE p.CreatedBY END) AS UserId,
                            p.CreatedDate
                        FROM dbo.Product p
                        LEFT JOIN dbo.Category c ON p.CategoryId = c.CategoryId
                        LEFT JOIN dbo.Brand b ON p.BrandId = b.BrandId
                        WHERE (CASE WHEN COL_LENGTH('dbo.Product', 'IsDelete') IS NOT NULL THEN ISNULL(p.IsDelete, 0) ELSE 0 END) = 0
                          AND (@BrandId IS NULL OR p.BrandId = @BrandId)
                          AND (@CategoryId IS NULL OR p.CategoryId = @CategoryId)
                          AND (@IsActive IS NULL OR (CASE WHEN COL_LENGTH('dbo.Product', 'IsActive') IS NOT NULL THEN ISNULL(p.IsActive, 1) ELSE 1 END) = @IsActive)
                          AND (@UserId IS NULL OR (CASE WHEN COL_LENGTH('dbo.Product', 'UserId') IS NOT NULL THEN ISNULL(p.UserId, ISNULL(p.CreatedBY, 1)) ELSE ISNULL(p.CreatedBY, 1) END) = @UserId)
                          AND (
                                @SearchText IS NULL 
                                OR p.[Name] LIKE '%' + @SearchText + '%' 
                                OR b.BrandName LIKE '%' + @SearchText + '%' 
                                OR c.CategoryName LIKE '%' + @SearchText + '%'
                          )
                        ORDER BY p.ProductId DESC
                        OFFSET @Offset ROWS
                        FETCH NEXT @PageSize ROWS ONLY;", connection))
                    {
                        cmd.Parameters.AddWithValue("@Offset", offset);
                        cmd.Parameters.AddWithValue("@PageSize", pageSize);
                        cmd.Parameters.AddWithValue("@SearchText", (object?)model.SearchText ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@BrandId", (object?)model.BrandId ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@CategoryId", (object?)model.CategoryId ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@IsActive", (object?)model.IsActive ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@UserId", (object?)model.UserId ?? DBNull.Value);

                        using var reader = await cmd.ExecuteReaderAsync();
                        response = reader.DataReaderMapToList<GetProductRes>();
                    }
                }
                catch (Exception ex)
                {
                    strDBError = JsonConvert.SerializeObject(ex);
                }
                finally
                {
                    if (connection.State == ConnectionState.Open) await connection.CloseAsync();
                }
            }

            return new Tuple<List<GetProductRes>, int, string>(response, TotalRecords, strDBError);
        }

        public async Task<Tuple<GetProductRes?, int, string>> GetProductById(GetProductByIdReq model)
        {
            string strDBError = string.Empty;
            GetProductRes? response = null;
            int totalRecords = 0;

            using (var connection = new SqlConnection(_connectionString))
            {
                try
                {
                    await connection.OpenAsync();
                    using var cmd = new SqlCommand(@"
                        SELECT 
                            p.ProductId,
                            p.[Name],
                            p.Price,
                            p.Size,
                            p.BrandId,
                            b.BrandName AS Brand,
                            p.CategoryId,
                            c.CategoryName AS Category,
                            (CASE WHEN COL_LENGTH('dbo.Product', 'IsActive') IS NOT NULL THEN ISNULL(p.IsActive, 1) ELSE 1 END) AS IsActive,
                            (CASE WHEN COL_LENGTH('dbo.Product', 'UserId') IS NOT NULL THEN p.UserId ELSE p.CreatedBY END) AS UserId,
                            p.CreatedDate
                        FROM dbo.Product p
                        LEFT JOIN dbo.Category c ON p.CategoryId = c.CategoryId
                        LEFT JOIN dbo.Brand b ON p.BrandId = b.BrandId
                        WHERE p.ProductId = @ProductId
                          AND (@UserId IS NULL OR (CASE WHEN COL_LENGTH('dbo.Product', 'UserId') IS NOT NULL THEN ISNULL(p.UserId, ISNULL(p.CreatedBY, 1)) ELSE ISNULL(p.CreatedBY, 1) END) = @UserId)
                          AND (CASE WHEN COL_LENGTH('dbo.Product', 'IsDelete') IS NOT NULL THEN ISNULL(p.IsDelete, 0) ELSE 0 END) = 0;", connection);

                    cmd.Parameters.AddWithValue("@ProductId", model.ProductId);
                    cmd.Parameters.AddWithValue("@UserId", (object?)model.UserId ?? DBNull.Value);

                    using var reader = await cmd.ExecuteReaderAsync();
                    response = reader.DataReaderMapToSingle<GetProductRes>();
                    totalRecords = response != null ? 1 : 0;
                }
                catch (Exception ex)
                {
                    strDBError = JsonConvert.SerializeObject(ex);
                }
                finally
                {
                    if (connection.State == ConnectionState.Open) await connection.CloseAsync();
                }
            }

            return new Tuple<GetProductRes?, int, string>(response, totalRecords, strDBError);
        }

        public async Task<Tuple<bool, int, string>> SaveProduct(SaveProductReq model)
        {
            string strDBError = string.Empty;
            bool success = false;

            using (var connection = new SqlConnection(_connectionString))
            {
                try
                {
                    await connection.OpenAsync();

                    if (model.ProductId <= 0)
                    {
                        // Insert with UserId
                        using var cmd = new SqlCommand(@"
                            INSERT INTO dbo.Product ([Name], BrandId, CategoryId, Price, Size, IsActive, IsDelete, UserId, CreatedBY, CreatedDate)
                            VALUES (@Name, @BrandId, @CategoryId, @Price, @Size, @IsActive, 0, @UserId, @UserId, GETDATE());
                            SELECT SCOPE_IDENTITY();", connection);

                        cmd.Parameters.AddWithValue("@Name", model.Name ?? "");
                        cmd.Parameters.AddWithValue("@BrandId", (object?)model.BrandId ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@CategoryId", (object?)model.CategoryId ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Price", model.Price);
                        cmd.Parameters.AddWithValue("@Size", (object?)model.Size ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@IsActive", model.IsActive ? 1 : 0);
                        cmd.Parameters.AddWithValue("@UserId", (object?)model.UserId ?? 1);

                        var result = await cmd.ExecuteScalarAsync();
                        success = result != null && Convert.ToInt32(result) > 0;
                    }
                    else
                    {
                        // Update scoped by UserId if specified
                        using var cmd = new SqlCommand(@"
                            UPDATE dbo.Product
                            SET [Name] = @Name,
                                Price = @Price,
                                Size = @Size,
                                IsActive = @IsActive
                            WHERE ProductId = @ProductId
                              AND (@UserId IS NULL OR ISNULL(UserId, @UserId) = @UserId);", connection);

                        cmd.Parameters.AddWithValue("@ProductId", model.ProductId);
                        cmd.Parameters.AddWithValue("@Name", model.Name ?? "");
                        cmd.Parameters.AddWithValue("@Price", model.Price);
                        cmd.Parameters.AddWithValue("@Size", (object?)model.Size ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@IsActive", model.IsActive ? 1 : 0);
                        cmd.Parameters.AddWithValue("@UserId", (object?)model.UserId ?? DBNull.Value);

                        int rows = await cmd.ExecuteNonQueryAsync();
                        success = rows > 0;
                    }
                }
                catch (Exception ex)
                {
                    strDBError = JsonConvert.SerializeObject(ex);
                }
                finally
                {
                    if (connection.State == ConnectionState.Open) await connection.CloseAsync();
                }
            }

            return new Tuple<bool, int, string>(success, success ? 1 : 0, strDBError);
        }

        public async Task<Tuple<ToggleStatusRes, int, string>> ToggleStatus(ToggleStatusReq model)
        {
            string strDBError = string.Empty;
            var res = new ToggleStatusRes { Id = model.Id };

            using (var connection = new SqlConnection(_connectionString))
            {
                try
                {
                    await connection.OpenAsync();

                    bool newStatus;
                    if (model.IsActive.HasValue)
                    {
                        newStatus = model.IsActive.Value;
                    }
                    else
                    {
                        using var getCmd = new SqlCommand("SELECT ISNULL(IsActive, 1) FROM dbo.Product WHERE ProductId = @Id;", connection);
                        getCmd.Parameters.AddWithValue("@Id", model.Id);
                        var currentVal = await getCmd.ExecuteScalarAsync();
                        newStatus = currentVal == null || !Convert.ToBoolean(currentVal);
                    }

                    using var updateCmd = new SqlCommand(@"
                        UPDATE dbo.Product 
                        SET IsActive = @IsActive 
                        WHERE ProductId = @Id
                          AND (@UserId IS NULL OR (CASE WHEN COL_LENGTH('dbo.Product', 'UserId') IS NOT NULL THEN ISNULL(UserId, @UserId) ELSE @UserId END) = @UserId);", connection);

                    updateCmd.Parameters.AddWithValue("@Id", model.Id);
                    updateCmd.Parameters.AddWithValue("@IsActive", newStatus ? 1 : 0);
                    updateCmd.Parameters.AddWithValue("@UserId", (object?)model.UserId ?? DBNull.Value);

                    int affected = await updateCmd.ExecuteNonQueryAsync();
                    if (affected > 0)
                    {
                        res.Success = true;
                        res.IsActive = newStatus;
                        res.Message = $"Product status changed to {(newStatus ? "ON (Active)" : "OFF (Inactive)")}.";
                    }
                    else
                    {
                        res.Success = false;
                        res.Message = "Product record not found or access denied.";
                    }
                }
                catch (Exception ex)
                {
                    strDBError = JsonConvert.SerializeObject(ex);
                    res.Success = false;
                    res.Message = "Database error while updating status.";
                }
                finally
                {
                    if (connection.State == ConnectionState.Open) await connection.CloseAsync();
                }
            }

            return new Tuple<ToggleStatusRes, int, string>(res, res.Success ? 1 : 0, strDBError);
        }

        public async Task<Tuple<DeleteRes, int, string>> DeleteProduct(DeleteReq model)
        {
            string strDBError = string.Empty;
            var res = new DeleteRes { Id = model.Id };

            using (var connection = new SqlConnection(_connectionString))
            {
                try
                {
                    await connection.OpenAsync();
                    using var cmd = new SqlCommand(@"
                        UPDATE dbo.Product 
                        SET IsDelete = 1, IsActive = 0 
                        WHERE ProductId = @Id
                          AND (@UserId IS NULL OR (CASE WHEN COL_LENGTH('dbo.Product', 'UserId') IS NOT NULL THEN ISNULL(UserId, @UserId) ELSE @UserId END) = @UserId);", connection);

                    cmd.Parameters.AddWithValue("@Id", model.Id);
                    cmd.Parameters.AddWithValue("@UserId", (object?)model.UserId ?? DBNull.Value);

                    int affected = await cmd.ExecuteNonQueryAsync();
                    if (affected > 0)
                    {
                        res.Success = true;
                        res.Message = "Product deleted successfully.";
                    }
                    else
                    {
                        res.Success = false;
                        res.Message = "Product not found or access denied.";
                    }
                }
                catch (Exception ex)
                {
                    strDBError = JsonConvert.SerializeObject(ex);
                    res.Success = false;
                    res.Message = "Database error while deleting product.";
                }
                finally
                {
                    if (connection.State == ConnectionState.Open) await connection.CloseAsync();
                }
            }

            return new Tuple<DeleteRes, int, string>(res, res.Success ? 1 : 0, strDBError);
        }

        public async Task<Tuple<int, int, string>> GetActiveProductCount(int? userId = null)
        {
            string strDBError = string.Empty;
            int count = 0;

            using (var connection = new SqlConnection(_connectionString))
            {
                try
                {
                    await connection.OpenAsync();
                    using var cmd = new SqlCommand(@"
                        SELECT COUNT(1) 
                        FROM dbo.Product 
                        WHERE (CASE WHEN COL_LENGTH('dbo.Product', 'IsDelete') IS NOT NULL THEN ISNULL(IsDelete, 0) ELSE 0 END) = 0
                          AND (CASE WHEN COL_LENGTH('dbo.Product', 'IsActive') IS NOT NULL THEN ISNULL(IsActive, 1) ELSE 1 END) = 1
                          AND (@UserId IS NULL OR (CASE WHEN COL_LENGTH('dbo.Product', 'UserId') IS NOT NULL THEN ISNULL(UserId, ISNULL(CreatedBY, 1)) ELSE ISNULL(CreatedBY, 1) END) = @UserId);", connection);

                    cmd.Parameters.AddWithValue("@UserId", (object?)userId ?? DBNull.Value);

                    count = Convert.ToInt32(await cmd.ExecuteScalarAsync() ?? 0);
                }
                catch (Exception ex)
                {
                    strDBError = JsonConvert.SerializeObject(ex);
                }
                finally
                {
                    if (connection.State == ConnectionState.Open) await connection.CloseAsync();
                }
            }

            return new Tuple<int, int, string>(count, count, strDBError);
        }
    }
}
