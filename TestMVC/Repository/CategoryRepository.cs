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
    public class CategoryRepository : ICategoryRepository
    {
        private readonly string _connectionString;

        public CategoryRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("DefaultConnection is missing in appsettings.json");

            DbInitializer.EnsureDatabaseSetup(_connectionString);
        }

        public async Task<Tuple<List<GetCategoryRes>, int, string>> GetCategory(GetCategoryReq model)
        {
            string strDBError = string.Empty;
            List<GetCategoryRes> response = new List<GetCategoryRes>();
            int TotalRecords = 0;

            using (var connection = new SqlConnection(_connectionString))
            {
                try
                {
                    await connection.OpenAsync();

                    using (var countCmd = new SqlCommand(@"
                        SELECT COUNT(1)
                        FROM dbo.Category c
                        WHERE (CASE WHEN COL_LENGTH('dbo.Category', 'IsDelete') IS NOT NULL THEN ISNULL(c.IsDelete, 0) ELSE 0 END) = 0
                          AND (@IsActive IS NULL OR (CASE WHEN COL_LENGTH('dbo.Category', 'IsActive') IS NOT NULL THEN ISNULL(c.IsActive, 1) ELSE 1 END) = @IsActive)
                          AND (@UserId IS NULL OR (CASE WHEN COL_LENGTH('dbo.Category', 'UserId') IS NOT NULL THEN ISNULL(c.UserId, 1) ELSE 1 END) = @UserId)
                          AND (@SearchText IS NULL OR c.CategoryName LIKE '%' + @SearchText + '%');", connection))
                    {
                        countCmd.Parameters.AddWithValue("@SearchText", (object?)model.SearchText ?? DBNull.Value);
                        countCmd.Parameters.AddWithValue("@IsActive", (object?)model.IsActive ?? DBNull.Value);
                        countCmd.Parameters.AddWithValue("@UserId", (object?)model.UserId ?? DBNull.Value);
                        TotalRecords = Convert.ToInt32(await countCmd.ExecuteScalarAsync() ?? 0);
                    }

                    int pageNo = model.PageNo > 0 ? model.PageNo : 1;
                    int pageSize = model.PageSize > 0 ? model.PageSize : 50;
                    int offset = (pageNo - 1) * pageSize;

                    using (var cmd = new SqlCommand(@"
                        SELECT 
                            c.CategoryId,
                            c.CategoryName,
                            (CASE WHEN COL_LENGTH('dbo.Category', 'IsActive') IS NOT NULL THEN ISNULL(c.IsActive, 1) ELSE 1 END) AS IsActive,
                            (CASE WHEN COL_LENGTH('dbo.Category', 'UserId') IS NOT NULL THEN c.UserId ELSE 1 END) AS UserId,
                            c.CreatedDate
                        FROM dbo.Category c
                        WHERE (CASE WHEN COL_LENGTH('dbo.Category', 'IsDelete') IS NOT NULL THEN ISNULL(c.IsDelete, 0) ELSE 0 END) = 0
                          AND (@IsActive IS NULL OR (CASE WHEN COL_LENGTH('dbo.Category', 'IsActive') IS NOT NULL THEN ISNULL(c.IsActive, 1) ELSE 1 END) = @IsActive)
                          AND (@UserId IS NULL OR (CASE WHEN COL_LENGTH('dbo.Category', 'UserId') IS NOT NULL THEN ISNULL(c.UserId, 1) ELSE 1 END) = @UserId)
                          AND (@SearchText IS NULL OR c.CategoryName LIKE '%' + @SearchText + '%')
                        ORDER BY c.CategoryId DESC
                        OFFSET @Offset ROWS
                        FETCH NEXT @PageSize ROWS ONLY;", connection))
                    {
                        cmd.Parameters.AddWithValue("@Offset", offset);
                        cmd.Parameters.AddWithValue("@PageSize", pageSize);
                        cmd.Parameters.AddWithValue("@SearchText", (object?)model.SearchText ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@IsActive", (object?)model.IsActive ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@UserId", (object?)model.UserId ?? DBNull.Value);

                        using var reader = await cmd.ExecuteReaderAsync();
                        response = reader.DataReaderMapToList<GetCategoryRes>();
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

            return new Tuple<List<GetCategoryRes>, int, string>(response, TotalRecords, strDBError);
        }

        public async Task<Tuple<GetCategoryRes?, int, string>> GetCategoryById(GetCategoryByIdReq model)
        {
            string strDBError = string.Empty;
            GetCategoryRes? response = null;
            int totalRecords = 0;

            using (var connection = new SqlConnection(_connectionString))
            {
                try
                {
                    await connection.OpenAsync();
                    using var cmd = new SqlCommand(@"
                        SELECT 
                            c.CategoryId,
                            c.CategoryName,
                            (CASE WHEN COL_LENGTH('dbo.Category', 'IsActive') IS NOT NULL THEN ISNULL(c.IsActive, 1) ELSE 1 END) AS IsActive,
                            (CASE WHEN COL_LENGTH('dbo.Category', 'UserId') IS NOT NULL THEN c.UserId ELSE 1 END) AS UserId,
                            c.CreatedDate
                        FROM dbo.Category c
                        WHERE c.CategoryId = @CategoryId
                          AND (@UserId IS NULL OR (CASE WHEN COL_LENGTH('dbo.Category', 'UserId') IS NOT NULL THEN ISNULL(c.UserId, 1) ELSE 1 END) = @UserId)
                          AND (CASE WHEN COL_LENGTH('dbo.Category', 'IsDelete') IS NOT NULL THEN ISNULL(c.IsDelete, 0) ELSE 0 END) = 0;", connection);

                    cmd.Parameters.AddWithValue("@CategoryId", model.CategoryId);
                    cmd.Parameters.AddWithValue("@UserId", (object?)model.UserId ?? DBNull.Value);

                    using var reader = await cmd.ExecuteReaderAsync();
                    response = reader.DataReaderMapToSingle<GetCategoryRes>();
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

            return new Tuple<GetCategoryRes?, int, string>(response, totalRecords, strDBError);
        }

        public async Task<Tuple<bool, int, string>> SaveCategory(SaveCategoryReq model)
        {
            string strDBError = string.Empty;
            bool success = false;

            using (var connection = new SqlConnection(_connectionString))
            {
                try
                {
                    await connection.OpenAsync();

                    if (model.CategoryId <= 0)
                    {
                        // Insert with UserId
                        using var cmd = new SqlCommand(@"
                            INSERT INTO dbo.Category (CategoryName, IsActive, IsDelete, UserId, CreatedDate)
                            VALUES (@CategoryName, @IsActive, 0, @UserId, GETDATE());
                            SELECT SCOPE_IDENTITY();", connection);

                        cmd.Parameters.AddWithValue("@CategoryName", model.CategoryName ?? "");
                        cmd.Parameters.AddWithValue("@IsActive", model.IsActive ? 1 : 0);
                        cmd.Parameters.AddWithValue("@UserId", (object?)model.UserId ?? 1);

                        var result = await cmd.ExecuteScalarAsync();
                        success = result != null && Convert.ToInt32(result) > 0;
                    }
                    else
                    {
                        // Update scoped by UserId if specified
                        using var cmd = new SqlCommand(@"
                            UPDATE dbo.Category
                            SET CategoryName = @CategoryName,
                                IsActive = @IsActive
                            WHERE CategoryId = @CategoryId
                              AND (@UserId IS NULL OR ISNULL(UserId, @UserId) = @UserId);", connection);

                        cmd.Parameters.AddWithValue("@CategoryId", model.CategoryId);
                        cmd.Parameters.AddWithValue("@CategoryName", model.CategoryName ?? "");
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
                        using var getCmd = new SqlCommand("SELECT ISNULL(IsActive, 1) FROM dbo.Category WHERE CategoryId = @Id;", connection);
                        getCmd.Parameters.AddWithValue("@Id", model.Id);
                        var currentVal = await getCmd.ExecuteScalarAsync();
                        newStatus = currentVal == null || !Convert.ToBoolean(currentVal);
                    }

                    using var updateCmd = new SqlCommand(@"
                        UPDATE dbo.Category 
                        SET IsActive = @IsActive 
                        WHERE CategoryId = @Id
                          AND (@UserId IS NULL OR (CASE WHEN COL_LENGTH('dbo.Category', 'UserId') IS NOT NULL THEN ISNULL(UserId, @UserId) ELSE @UserId END) = @UserId);", connection);

                    updateCmd.Parameters.AddWithValue("@Id", model.Id);
                    updateCmd.Parameters.AddWithValue("@IsActive", newStatus ? 1 : 0);
                    updateCmd.Parameters.AddWithValue("@UserId", (object?)model.UserId ?? DBNull.Value);

                    int affected = await updateCmd.ExecuteNonQueryAsync();
                    if (affected > 0)
                    {
                        res.Success = true;
                        res.IsActive = newStatus;
                        res.Message = $"Category status changed to {(newStatus ? "ON (Active)" : "OFF (Inactive)")}.";
                    }
                    else
                    {
                        res.Success = false;
                        res.Message = "Category record not found or access denied.";
                    }
                }
                catch (Exception ex)
                {
                    strDBError = JsonConvert.SerializeObject(ex);
                    res.Success = false;
                    res.Message = "Database error while updating category status.";
                }
                finally
                {
                    if (connection.State == ConnectionState.Open) await connection.CloseAsync();
                }
            }

            return new Tuple<ToggleStatusRes, int, string>(res, res.Success ? 1 : 0, strDBError);
        }

        public async Task<Tuple<DeleteRes, int, string>> DeleteCategory(DeleteReq model)
        {
            string strDBError = string.Empty;
            var res = new DeleteRes { Id = model.Id };

            using (var connection = new SqlConnection(_connectionString))
            {
                try
                {
                    await connection.OpenAsync();
                    using var cmd = new SqlCommand(@"
                        UPDATE dbo.Category 
                        SET IsDelete = 1, IsActive = 0 
                        WHERE CategoryId = @Id
                          AND (@UserId IS NULL OR (CASE WHEN COL_LENGTH('dbo.Category', 'UserId') IS NOT NULL THEN ISNULL(UserId, @UserId) ELSE @UserId END) = @UserId);", connection);

                    cmd.Parameters.AddWithValue("@Id", model.Id);
                    cmd.Parameters.AddWithValue("@UserId", (object?)model.UserId ?? DBNull.Value);

                    int affected = await cmd.ExecuteNonQueryAsync();
                    if (affected > 0)
                    {
                        res.Success = true;
                        res.Message = "Category deleted successfully.";
                    }
                    else
                    {
                        res.Success = false;
                        res.Message = "Category not found or access denied.";
                    }
                }
                catch (Exception ex)
                {
                    strDBError = JsonConvert.SerializeObject(ex);
                    res.Success = false;
                    res.Message = "Database error while deleting category.";
                }
                finally
                {
                    if (connection.State == ConnectionState.Open) await connection.CloseAsync();
                }
            }

            return new Tuple<DeleteRes, int, string>(res, res.Success ? 1 : 0, strDBError);
        }

        public async Task<Tuple<int, int, string>> GetActiveCategoryCount(int? userId = null)
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
                        FROM dbo.Category 
                        WHERE (CASE WHEN COL_LENGTH('dbo.Category', 'IsDelete') IS NOT NULL THEN ISNULL(IsDelete, 0) ELSE 0 END) = 0
                          AND (CASE WHEN COL_LENGTH('dbo.Category', 'IsActive') IS NOT NULL THEN ISNULL(IsActive, 1) ELSE 1 END) = 1
                          AND (@UserId IS NULL OR (CASE WHEN COL_LENGTH('dbo.Category', 'UserId') IS NOT NULL THEN ISNULL(UserId, 1) ELSE 1 END) = @UserId);", connection);

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
