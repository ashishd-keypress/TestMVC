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
                        LEFT JOIN dbo.ProductModel pm ON p.ProductModelId = pm.ProductModelId
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
                                OR p.SKU LIKE '%' + @SearchText + '%'
                                OR p.Barcode LIKE '%' + @SearchText + '%'
                                OR p.RAM LIKE '%' + @SearchText + '%'
                                OR p.Storage LIKE '%' + @SearchText + '%'
                                OR p.Color LIKE '%' + @SearchText + '%'
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
                            (CASE 
                                WHEN COL_LENGTH('dbo.Product', 'Image') IS NOT NULL AND p.[Image] IS NOT NULL THEN p.[Image]
                                WHEN COL_LENGTH('dbo.Product', 'ImageUrl') IS NOT NULL AND p.ImageUrl IS NOT NULL THEN p.ImageUrl
                                WHEN COL_LENGTH('dbo.Product', 'ImagePath') IS NOT NULL AND p.ImagePath IS NOT NULL THEN p.ImagePath
                                ELSE NULL 
                            END) AS [Image],
                            (CASE 
                                WHEN COL_LENGTH('dbo.Product', 'Image') IS NOT NULL AND p.[Image] IS NOT NULL THEN p.[Image]
                                WHEN COL_LENGTH('dbo.Product', 'ImageUrl') IS NOT NULL AND p.ImageUrl IS NOT NULL THEN p.ImageUrl
                                WHEN COL_LENGTH('dbo.Product', 'ImagePath') IS NOT NULL AND p.ImagePath IS NOT NULL THEN p.ImagePath
                                ELSE NULL 
                            END) AS ImageUrl,
                            (CASE 
                                WHEN COL_LENGTH('dbo.Product', 'Image') IS NOT NULL AND p.[Image] IS NOT NULL THEN p.[Image]
                                WHEN COL_LENGTH('dbo.Product', 'ImageUrl') IS NOT NULL AND p.ImageUrl IS NOT NULL THEN p.ImageUrl
                                WHEN COL_LENGTH('dbo.Product', 'ImagePath') IS NOT NULL AND p.ImagePath IS NOT NULL THEN p.ImagePath
                                ELSE NULL 
                            END) AS ImagePath,
                            p.Price,
                            p.MRP,
                            p.SellingPrice,
                            p.RAM,
                            p.Storage,
                            p.Color,
                            p.SKU,
                            p.Barcode,
                            ISNULL(p.WarrantyMonths, 12) AS WarrantyMonths,
                            ISNULL(p.TrackIMEI, 0) AS TrackIMEI,
                            p.ProductModelId,
                            pm.ModelName,
                            ISNULL((SELECT SUM(sb.QuantityOnHand) FROM dbo.StockBalance sb WHERE sb.ProductId = p.ProductId), 0) AS StockQuantity,
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
                        LEFT JOIN dbo.ProductModel pm ON p.ProductModelId = pm.ProductModelId
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
                                OR p.SKU LIKE '%' + @SearchText + '%'
                                OR p.Barcode LIKE '%' + @SearchText + '%'
                                OR p.RAM LIKE '%' + @SearchText + '%'
                                OR p.Storage LIKE '%' + @SearchText + '%'
                                OR p.Color LIKE '%' + @SearchText + '%'
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
                            (CASE 
                                WHEN COL_LENGTH('dbo.Product', 'Image') IS NOT NULL AND p.[Image] IS NOT NULL THEN p.[Image]
                                WHEN COL_LENGTH('dbo.Product', 'ImageUrl') IS NOT NULL AND p.ImageUrl IS NOT NULL THEN p.ImageUrl
                                WHEN COL_LENGTH('dbo.Product', 'ImagePath') IS NOT NULL AND p.ImagePath IS NOT NULL THEN p.ImagePath
                                ELSE NULL 
                            END) AS [Image],
                            (CASE 
                                WHEN COL_LENGTH('dbo.Product', 'Image') IS NOT NULL AND p.[Image] IS NOT NULL THEN p.[Image]
                                WHEN COL_LENGTH('dbo.Product', 'ImageUrl') IS NOT NULL AND p.ImageUrl IS NOT NULL THEN p.ImageUrl
                                WHEN COL_LENGTH('dbo.Product', 'ImagePath') IS NOT NULL AND p.ImagePath IS NOT NULL THEN p.ImagePath
                                ELSE NULL 
                            END) AS ImageUrl,
                            (CASE 
                                WHEN COL_LENGTH('dbo.Product', 'Image') IS NOT NULL AND p.[Image] IS NOT NULL THEN p.[Image]
                                WHEN COL_LENGTH('dbo.Product', 'ImageUrl') IS NOT NULL AND p.ImageUrl IS NOT NULL THEN p.ImageUrl
                                WHEN COL_LENGTH('dbo.Product', 'ImagePath') IS NOT NULL AND p.ImagePath IS NOT NULL THEN p.ImagePath
                                ELSE NULL 
                            END) AS ImagePath,
                            p.Price,
                            p.MRP,
                            p.SellingPrice,
                            p.RAM,
                            p.Storage,
                            p.Color,
                            p.SKU,
                            p.Barcode,
                            ISNULL(p.WarrantyMonths, 12) AS WarrantyMonths,
                            ISNULL(p.TrackIMEI, 0) AS TrackIMEI,
                            p.ProductModelId,
                            pm.ModelName,
                            ISNULL((SELECT SUM(sb.QuantityOnHand) FROM dbo.StockBalance sb WHERE sb.ProductId = p.ProductId), 0) AS StockQuantity,
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
                        LEFT JOIN dbo.ProductModel pm ON p.ProductModelId = pm.ProductModelId
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

                    var imgVal = model.Image ?? model.ImageUrl ?? model.ImagePath;
                    decimal effectivePrice = model.Price > 0 ? model.Price : (model.SellingPrice ?? 0);
                    decimal? effectiveSellingPrice = model.SellingPrice ?? (model.Price > 0 ? model.Price : null);
                    decimal? effectiveMRP = model.MRP ?? effectiveSellingPrice;

                    if (model.ProductId <= 0)
                    {
                        using var cmd = new SqlCommand(@"
                            INSERT INTO dbo.Product (
                                [Name], [Image], ImageUrl, ImagePath, BrandId, CategoryId, 
                                Price, MRP, SellingPrice, Size, RAM, Storage, Color, SKU, Barcode, 
                                WarrantyMonths, TrackIMEI, ProductModelId,
                                IsActive, IsDelete, UserId, CreatedBY, CreatedDate
                            )
                            VALUES (
                                @Name, @Image, @Image, @Image, @BrandId, @CategoryId,
                                @Price, @MRP, @SellingPrice, @Size, @RAM, @Storage, @Color, @SKU, @Barcode,
                                @WarrantyMonths, @TrackIMEI, @ProductModelId,
                                @IsActive, 0, @UserId, @UserId, GETDATE()
                            );
                            SELECT SCOPE_IDENTITY();", connection);

                        cmd.Parameters.AddWithValue("@Name", model.Name ?? "");
                        cmd.Parameters.AddWithValue("@Image", (object?)imgVal ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@BrandId", (object?)model.BrandId ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@CategoryId", (object?)model.CategoryId ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Price", effectivePrice);
                        cmd.Parameters.AddWithValue("@MRP", (object?)effectiveMRP ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@SellingPrice", (object?)effectiveSellingPrice ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Size", (object?)model.Size ?? "");
                        cmd.Parameters.AddWithValue("@RAM", (object?)model.RAM ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Storage", (object?)model.Storage ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Color", (object?)model.Color ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@SKU", (object?)model.SKU ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Barcode", (object?)model.Barcode ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@WarrantyMonths", model.WarrantyMonths > 0 ? model.WarrantyMonths : 12);
                        cmd.Parameters.AddWithValue("@TrackIMEI", model.TrackIMEI ? 1 : 0);
                        cmd.Parameters.AddWithValue("@ProductModelId", (object?)model.ProductModelId ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@IsActive", model.IsActive ? 1 : 0);
                        cmd.Parameters.AddWithValue("@UserId", (object?)model.UserId ?? 1);

                        var result = await cmd.ExecuteScalarAsync();
                        success = result != null && Convert.ToInt32(result) > 0;
                    }
                    else
                    {
                        using var cmd = new SqlCommand(@"
                            UPDATE dbo.Product
                            SET [Name] = @Name,
                                [Image] = COALESCE(@Image, [Image]),
                                ImageUrl = COALESCE(@Image, ImageUrl),
                                ImagePath = COALESCE(@Image, ImagePath),
                                BrandId = @BrandId,
                                CategoryId = @CategoryId,
                                Price = @Price,
                                MRP = @MRP,
                                SellingPrice = @SellingPrice,
                                Size = @Size,
                                RAM = @RAM,
                                Storage = @Storage,
                                Color = @Color,
                                SKU = @SKU,
                                Barcode = @Barcode,
                                WarrantyMonths = @WarrantyMonths,
                                TrackIMEI = @TrackIMEI,
                                ProductModelId = @ProductModelId,
                                IsActive = @IsActive
                            WHERE ProductId = @ProductId
                              AND (@UserId IS NULL OR ISNULL(UserId, @UserId) = @UserId);", connection);

                        cmd.Parameters.AddWithValue("@ProductId", model.ProductId);
                        cmd.Parameters.AddWithValue("@Name", model.Name ?? "");
                        cmd.Parameters.AddWithValue("@Image", (object?)imgVal ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@BrandId", (object?)model.BrandId ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@CategoryId", (object?)model.CategoryId ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Price", effectivePrice);
                        cmd.Parameters.AddWithValue("@MRP", (object?)effectiveMRP ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@SellingPrice", (object?)effectiveSellingPrice ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Size", (object?)model.Size ?? "");
                        cmd.Parameters.AddWithValue("@RAM", (object?)model.RAM ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Storage", (object?)model.Storage ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Color", (object?)model.Color ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@SKU", (object?)model.SKU ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@Barcode", (object?)model.Barcode ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@WarrantyMonths", model.WarrantyMonths > 0 ? model.WarrantyMonths : 12);
                        cmd.Parameters.AddWithValue("@TrackIMEI", model.TrackIMEI ? 1 : 0);
                        cmd.Parameters.AddWithValue("@ProductModelId", (object?)model.ProductModelId ?? DBNull.Value);
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
