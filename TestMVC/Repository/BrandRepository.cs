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
    public class BrandRepository : IBrandRepository
    {
        private readonly string _connectionString;

        public BrandRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("DefaultConnection is missing in appsettings.json");

            DbInitializer.EnsureDatabaseSetup(_connectionString);
        }

        public async Task<Tuple<List<GetBrandRes>, int, string>> GetBrand(GetBrandReq model)
        {
            string strDBError = string.Empty;
            List<GetBrandRes> response = new List<GetBrandRes>();
            int TotalRecords = 0;

            using (var connection = new SqlConnection(_connectionString))
            {
                try
                {
                    await connection.OpenAsync();

                    using (var countCmd = new SqlCommand(@"
                        SELECT COUNT(1)
                        FROM dbo.Brand b
                        WHERE (CASE WHEN COL_LENGTH('dbo.Brand', 'IsDelete') IS NOT NULL THEN ISNULL(b.IsDelete, 0) ELSE 0 END) = 0
                          AND (@IsActive IS NULL OR (CASE WHEN COL_LENGTH('dbo.Brand', 'IsActive') IS NOT NULL THEN ISNULL(b.IsActive, 1) ELSE 1 END) = @IsActive)
                          AND (@UserId IS NULL OR (CASE WHEN COL_LENGTH('dbo.Brand', 'UserId') IS NOT NULL THEN ISNULL(b.UserId, 1) ELSE 1 END) = @UserId)
                          AND (@SearchText IS NULL OR b.BrandName LIKE '%' + @SearchText + '%');", connection))
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
                            b.BrandId,
                            b.BrandName,
                            (CASE 
                                WHEN COL_LENGTH('dbo.Brand', 'Image') IS NOT NULL AND b.[Image] IS NOT NULL THEN b.[Image]
                                WHEN COL_LENGTH('dbo.Brand', 'ImageUrl') IS NOT NULL AND b.ImageUrl IS NOT NULL THEN b.ImageUrl
                                ELSE NULL 
                            END) AS [Image],
                            (CASE 
                                WHEN COL_LENGTH('dbo.Brand', 'Image') IS NOT NULL AND b.[Image] IS NOT NULL THEN b.[Image]
                                WHEN COL_LENGTH('dbo.Brand', 'ImageUrl') IS NOT NULL AND b.ImageUrl IS NOT NULL THEN b.ImageUrl
                                ELSE NULL 
                            END) AS ImageUrl,
                            (CASE WHEN COL_LENGTH('dbo.Brand', 'IsActive') IS NOT NULL THEN ISNULL(b.IsActive, 1) ELSE 1 END) AS IsActive,
                            (CASE WHEN COL_LENGTH('dbo.Brand', 'UserId') IS NOT NULL THEN b.UserId ELSE 1 END) AS UserId,
                            b.CreatedDate
                        FROM dbo.Brand b
                        WHERE (CASE WHEN COL_LENGTH('dbo.Brand', 'IsDelete') IS NOT NULL THEN ISNULL(b.IsDelete, 0) ELSE 0 END) = 0
                          AND (@IsActive IS NULL OR (CASE WHEN COL_LENGTH('dbo.Brand', 'IsActive') IS NOT NULL THEN ISNULL(b.IsActive, 1) ELSE 1 END) = @IsActive)
                          AND (@UserId IS NULL OR (CASE WHEN COL_LENGTH('dbo.Brand', 'UserId') IS NOT NULL THEN ISNULL(b.UserId, 1) ELSE 1 END) = @UserId)
                          AND (@SearchText IS NULL OR b.BrandName LIKE '%' + @SearchText + '%')
                        ORDER BY b.BrandId DESC
                        OFFSET @Offset ROWS
                        FETCH NEXT @PageSize ROWS ONLY;", connection))
                    {
                        cmd.Parameters.AddWithValue("@Offset", offset);
                        cmd.Parameters.AddWithValue("@PageSize", pageSize);
                        cmd.Parameters.AddWithValue("@SearchText", (object?)model.SearchText ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@IsActive", (object?)model.IsActive ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@UserId", (object?)model.UserId ?? DBNull.Value);

                        using var reader = await cmd.ExecuteReaderAsync();
                        response = reader.DataReaderMapToList<GetBrandRes>();
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

            return new Tuple<List<GetBrandRes>, int, string>(response, TotalRecords, strDBError);
        }

        public async Task<Tuple<GetBrandRes?, int, string>> GetBrandById(GetBrandByIdReq model)
        {
            string strDBError = string.Empty;
            GetBrandRes? response = null;
            int totalRecords = 0;

            using (var connection = new SqlConnection(_connectionString))
            {
                try
                {
                    await connection.OpenAsync();
                    using var cmd = new SqlCommand(@"
                        SELECT 
                            b.BrandId,
                            b.BrandName,
                            (CASE 
                                WHEN COL_LENGTH('dbo.Brand', 'Image') IS NOT NULL AND b.[Image] IS NOT NULL THEN b.[Image]
                                WHEN COL_LENGTH('dbo.Brand', 'ImageUrl') IS NOT NULL AND b.ImageUrl IS NOT NULL THEN b.ImageUrl
                                ELSE NULL 
                            END) AS [Image],
                            (CASE 
                                WHEN COL_LENGTH('dbo.Brand', 'Image') IS NOT NULL AND b.[Image] IS NOT NULL THEN b.[Image]
                                WHEN COL_LENGTH('dbo.Brand', 'ImageUrl') IS NOT NULL AND b.ImageUrl IS NOT NULL THEN b.ImageUrl
                                ELSE NULL 
                            END) AS ImageUrl,
                            (CASE WHEN COL_LENGTH('dbo.Brand', 'IsActive') IS NOT NULL THEN ISNULL(b.IsActive, 1) ELSE 1 END) AS IsActive,
                            (CASE WHEN COL_LENGTH('dbo.Brand', 'UserId') IS NOT NULL THEN b.UserId ELSE 1 END) AS UserId,
                            b.CreatedDate
                        FROM dbo.Brand b
                        WHERE b.BrandId = @BrandId
                          AND (@UserId IS NULL OR (CASE WHEN COL_LENGTH('dbo.Brand', 'UserId') IS NOT NULL THEN ISNULL(b.UserId, 1) ELSE 1 END) = @UserId)
                          AND (CASE WHEN COL_LENGTH('dbo.Brand', 'IsDelete') IS NOT NULL THEN ISNULL(b.IsDelete, 0) ELSE 0 END) = 0;", connection);

                    cmd.Parameters.AddWithValue("@BrandId", model.BrandId);
                    cmd.Parameters.AddWithValue("@UserId", (object?)model.UserId ?? DBNull.Value);

                    using var reader = await cmd.ExecuteReaderAsync();
                    response = reader.DataReaderMapToSingle<GetBrandRes>();
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

            return new Tuple<GetBrandRes?, int, string>(response, totalRecords, strDBError);
        }

        public async Task<Tuple<bool, int, string>> SaveBrand(SaveBrandReq model)
        {
            string strDBError = string.Empty;
            bool success = false;
            string trimmedName = (model.BrandName ?? "").Trim();
            string? imageValue = model.Image ?? model.ImageUrl;

            using (var connection = new SqlConnection(_connectionString))
            {
                try
                {
                    await connection.OpenAsync();

                    // Check for existing brand with same name
                    using (var checkCmd = new SqlCommand(@"
                        SELECT TOP 1 BrandId, IsDelete 
                        FROM dbo.Brand 
                        WHERE LOWER(LTRIM(RTRIM(BrandName))) = LOWER(@BrandName)
                          AND (@BrandId = 0 OR BrandId != @BrandId);", connection))
                    {
                        checkCmd.Parameters.AddWithValue("@BrandName", trimmedName);
                        checkCmd.Parameters.AddWithValue("@BrandId", model.BrandId);

                        int existingId = 0;
                        bool existingDeleted = false;
                        using (var reader = await checkCmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                existingId = Convert.ToInt32(reader["BrandId"]);
                                existingDeleted = Convert.ToBoolean(reader["IsDelete"]);
                            }
                        }

                        if (model.BrandId <= 0)
                        {
                            if (existingId > 0)
                            {
                                if (existingDeleted)
                                {
                                    // Reactivate previously deleted brand
                                    using var reactivateCmd = new SqlCommand(@"
                                        UPDATE dbo.Brand
                                        SET BrandName = @BrandName,
                                            [Image] = COALESCE(@Image, [Image]),
                                            ImageUrl = COALESCE(@Image, ImageUrl),
                                            IsActive = @IsActive,
                                            IsDelete = 0,
                                            UserId = @UserId,
                                            CreatedDate = GETDATE()
                                        WHERE BrandId = @ExistingId;", connection);

                                    reactivateCmd.Parameters.AddWithValue("@ExistingId", existingId);
                                    reactivateCmd.Parameters.AddWithValue("@BrandName", trimmedName);
                                    reactivateCmd.Parameters.AddWithValue("@Image", (object?)imageValue ?? DBNull.Value);
                                    reactivateCmd.Parameters.AddWithValue("@IsActive", model.IsActive ? 1 : 0);
                                    reactivateCmd.Parameters.AddWithValue("@UserId", (object?)model.UserId ?? 1);

                                    int rows = await reactivateCmd.ExecuteNonQueryAsync();
                                    success = rows > 0;
                                }
                                else
                                {
                                    return new Tuple<bool, int, string>(false, 0, $"A brand with the name '{trimmedName}' already exists.");
                                }
                            }
                            else
                            {
                                // Insert fresh brand
                                using var cmd = new SqlCommand(@"
                                    INSERT INTO dbo.Brand (BrandName, [Image], ImageUrl, IsActive, IsDelete, UserId, CreatedDate)
                                    VALUES (@BrandName, @Image, @Image, @IsActive, 0, @UserId, GETDATE());
                                    SELECT SCOPE_IDENTITY();", connection);

                                cmd.Parameters.AddWithValue("@BrandName", trimmedName);
                                cmd.Parameters.AddWithValue("@Image", (object?)imageValue ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@IsActive", model.IsActive ? 1 : 0);
                                cmd.Parameters.AddWithValue("@UserId", (object?)model.UserId ?? 1);

                                var result = await cmd.ExecuteScalarAsync();
                                success = result != null && Convert.ToInt32(result) > 0;
                            }
                        }
                        else
                        {
                            if (existingId > 0 && !existingDeleted)
                            {
                                return new Tuple<bool, int, string>(false, 0, $"A brand with the name '{trimmedName}' already exists.");
                            }

                            // Update scoped by UserId if specified
                            using var cmd = new SqlCommand(@"
                                UPDATE dbo.Brand
                                SET BrandName = @BrandName,
                                    [Image] = COALESCE(@Image, [Image]),
                                    ImageUrl = COALESCE(@Image, ImageUrl),
                                    IsActive = @IsActive
                                WHERE BrandId = @BrandId
                                  AND (@UserId IS NULL OR ISNULL(UserId, @UserId) = @UserId);", connection);

                            cmd.Parameters.AddWithValue("@BrandId", model.BrandId);
                            cmd.Parameters.AddWithValue("@BrandName", trimmedName);
                            cmd.Parameters.AddWithValue("@Image", (object?)imageValue ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@IsActive", model.IsActive ? 1 : 0);
                            cmd.Parameters.AddWithValue("@UserId", (object?)model.UserId ?? DBNull.Value);

                            int rows = await cmd.ExecuteNonQueryAsync();
                            success = rows > 0;
                        }
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
                        using var getCmd = new SqlCommand("SELECT ISNULL(IsActive, 1) FROM dbo.Brand WHERE BrandId = @Id;", connection);
                        getCmd.Parameters.AddWithValue("@Id", model.Id);
                        var currentVal = await getCmd.ExecuteScalarAsync();
                        newStatus = currentVal == null || !Convert.ToBoolean(currentVal);
                    }

                    using var updateCmd = new SqlCommand(@"
                        UPDATE dbo.Brand 
                        SET IsActive = @IsActive 
                        WHERE BrandId = @Id
                          AND (@UserId IS NULL OR (CASE WHEN COL_LENGTH('dbo.Brand', 'UserId') IS NOT NULL THEN ISNULL(UserId, @UserId) ELSE @UserId END) = @UserId);", connection);

                    updateCmd.Parameters.AddWithValue("@Id", model.Id);
                    updateCmd.Parameters.AddWithValue("@IsActive", newStatus ? 1 : 0);
                    updateCmd.Parameters.AddWithValue("@UserId", (object?)model.UserId ?? DBNull.Value);

                    int affected = await updateCmd.ExecuteNonQueryAsync();
                    if (affected > 0)
                    {
                        res.Success = true;
                        res.IsActive = newStatus;
                        res.Message = $"Brand status changed to {(newStatus ? "ON (Active)" : "OFF (Inactive)")}.";
                    }
                    else
                    {
                        res.Success = false;
                        res.Message = "Brand record not found or access denied.";
                    }
                }
                catch (Exception ex)
                {
                    strDBError = JsonConvert.SerializeObject(ex);
                    res.Success = false;
                    res.Message = "Database error while updating brand status.";
                }
                finally
                {
                    if (connection.State == ConnectionState.Open) await connection.CloseAsync();
                }
            }

            return new Tuple<ToggleStatusRes, int, string>(res, res.Success ? 1 : 0, strDBError);
        }

        public async Task<Tuple<DeleteRes, int, string>> DeleteBrand(DeleteReq model)
        {
            string strDBError = string.Empty;
            var res = new DeleteRes { Id = model.Id };

            using (var connection = new SqlConnection(_connectionString))
            {
                try
                {
                    await connection.OpenAsync();
                    using var cmd = new SqlCommand(@"
                        UPDATE dbo.Brand 
                        SET IsDelete = 1, IsActive = 0 
                        WHERE BrandId = @Id
                          AND (@UserId IS NULL OR (CASE WHEN COL_LENGTH('dbo.Brand', 'UserId') IS NOT NULL THEN ISNULL(UserId, @UserId) ELSE @UserId END) = @UserId);", connection);

                    cmd.Parameters.AddWithValue("@Id", model.Id);
                    cmd.Parameters.AddWithValue("@UserId", (object?)model.UserId ?? DBNull.Value);

                    int affected = await cmd.ExecuteNonQueryAsync();
                    if (affected > 0)
                    {
                        res.Success = true;
                        res.Message = "Brand deleted successfully.";
                    }
                    else
                    {
                        res.Success = false;
                        res.Message = "Brand not found or access denied.";
                    }
                }
                catch (Exception ex)
                {
                    strDBError = JsonConvert.SerializeObject(ex);
                    res.Success = false;
                    res.Message = "Database error while deleting brand.";
                }
                finally
                {
                    if (connection.State == ConnectionState.Open) await connection.CloseAsync();
                }
            }

            return new Tuple<DeleteRes, int, string>(res, res.Success ? 1 : 0, strDBError);
        }

        public async Task<Tuple<int, int, string>> GetActiveBrandCount(int? userId = null)
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
                        FROM dbo.Brand 
                        WHERE (CASE WHEN COL_LENGTH('dbo.Brand', 'IsDelete') IS NOT NULL THEN ISNULL(IsDelete, 0) ELSE 0 END) = 0
                          AND (CASE WHEN COL_LENGTH('dbo.Brand', 'IsActive') IS NOT NULL THEN ISNULL(IsActive, 1) ELSE 1 END) = 1
                          AND (@UserId IS NULL OR (CASE WHEN COL_LENGTH('dbo.Brand', 'UserId') IS NOT NULL THEN ISNULL(UserId, 1) ELSE 1 END) = @UserId);", connection);

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
