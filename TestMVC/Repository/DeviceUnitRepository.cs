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
    public class DeviceUnitRepository : IDeviceUnitRepository
    {
        private readonly string _connectionString;

        public DeviceUnitRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("DefaultConnection is missing in appsettings.json");
        }

        public async Task<List<DeviceUnit>> GetDeviceUnits(string? search = null, byte? status = null, int? productId = null)
        {
            var list = new List<DeviceUnit>();
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var cmd = new SqlCommand(@"
                SELECT 
                    du.DeviceUnitId,
                    du.ProductId,
                    p.[Name] AS ProductName,
                    b.BrandName AS Brand,
                    c.CategoryName AS Category,
                    p.RAM,
                    p.Storage,
                    p.Color,
                    du.PurchaseItemId,
                    du.IMEI1,
                    du.IMEI2,
                    du.SerialNumber,
                    du.WarehouseId,
                    w.WarehouseName,
                    du.CurrentStatus,
                    du.CostPrice,
                    p.Price AS SellingPrice,
                    du.WarrantyStartDate,
                    du.WarrantyEndDate,
                    du.Notes,
                    du.CreatedDate
                FROM dbo.DeviceUnit du
                JOIN dbo.Product p ON du.ProductId = p.ProductId
                LEFT JOIN dbo.Brand b ON p.BrandId = b.BrandId
                LEFT JOIN dbo.Category c ON p.CategoryId = c.CategoryId
                LEFT JOIN dbo.Warehouse w ON du.WarehouseId = w.WarehouseId
                WHERE (@Status IS NULL OR du.CurrentStatus = @Status)
                  AND (@ProductId IS NULL OR du.ProductId = @ProductId)
                  AND (
                        @Search IS NULL 
                        OR du.IMEI1 LIKE '%' + @Search + '%' 
                        OR du.IMEI2 LIKE '%' + @Search + '%' 
                        OR du.SerialNumber LIKE '%' + @Search + '%' 
                        OR p.[Name] LIKE '%' + @Search + '%'
                        OR b.BrandName LIKE '%' + @Search + '%'
                  )
                ORDER BY du.DeviceUnitId DESC;", connection);

            cmd.Parameters.AddWithValue("@Status", (object?)status ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ProductId", (object?)productId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Search", (object?)search ?? DBNull.Value);

            using var reader = await cmd.ExecuteReaderAsync();
            list = reader.DataReaderMapToList<DeviceUnit>();
            return list;
        }

        public async Task<DeviceUnit?> GetDeviceUnitById(long deviceUnitId)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var cmd = new SqlCommand(@"
                SELECT 
                    du.DeviceUnitId,
                    du.ProductId,
                    p.[Name] AS ProductName,
                    b.BrandName AS Brand,
                    c.CategoryName AS Category,
                    p.RAM,
                    p.Storage,
                    p.Color,
                    du.PurchaseItemId,
                    du.IMEI1,
                    du.IMEI2,
                    du.SerialNumber,
                    du.WarehouseId,
                    w.WarehouseName,
                    du.CurrentStatus,
                    du.CostPrice,
                    p.Price AS SellingPrice,
                    du.WarrantyStartDate,
                    du.WarrantyEndDate,
                    du.Notes,
                    du.CreatedDate
                FROM dbo.DeviceUnit du
                JOIN dbo.Product p ON du.ProductId = p.ProductId
                LEFT JOIN dbo.Brand b ON p.BrandId = b.BrandId
                LEFT JOIN dbo.Category c ON p.CategoryId = c.CategoryId
                LEFT JOIN dbo.Warehouse w ON du.WarehouseId = w.WarehouseId
                WHERE du.DeviceUnitId = @Id;", connection);

            cmd.Parameters.AddWithValue("@Id", deviceUnitId);

            using var reader = await cmd.ExecuteReaderAsync();
            return reader.DataReaderMapToSingle<DeviceUnit>();
        }

        public async Task<DeviceUnit?> GetDeviceByIMEI(string imei)
        {
            if (string.IsNullOrWhiteSpace(imei)) return null;

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var cmd = new SqlCommand(@"
                SELECT TOP 1
                    du.DeviceUnitId,
                    du.ProductId,
                    p.[Name] AS ProductName,
                    b.BrandName AS Brand,
                    c.CategoryName AS Category,
                    p.RAM,
                    p.Storage,
                    p.Color,
                    du.PurchaseItemId,
                    du.IMEI1,
                    du.IMEI2,
                    du.SerialNumber,
                    du.WarehouseId,
                    w.WarehouseName,
                    du.CurrentStatus,
                    du.CostPrice,
                    p.Price AS SellingPrice,
                    du.WarrantyStartDate,
                    du.WarrantyEndDate,
                    du.Notes,
                    du.CreatedDate
                FROM dbo.DeviceUnit du
                JOIN dbo.Product p ON du.ProductId = p.ProductId
                LEFT JOIN dbo.Brand b ON p.BrandId = b.BrandId
                LEFT JOIN dbo.Category c ON p.CategoryId = c.CategoryId
                LEFT JOIN dbo.Warehouse w ON du.WarehouseId = w.WarehouseId
                WHERE du.IMEI1 = @IMEI OR du.IMEI2 = @IMEI OR du.SerialNumber = @IMEI;", connection);

            cmd.Parameters.AddWithValue("@IMEI", imei.Trim());

            using var reader = await cmd.ExecuteReaderAsync();
            return reader.DataReaderMapToSingle<DeviceUnit>();
        }

        public async Task<List<DeviceUnit>> GetAvailableDevicesForProduct(int productId)
        {
            var list = new List<DeviceUnit>();
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var cmd = new SqlCommand(@"
                SELECT 
                    du.DeviceUnitId,
                    du.ProductId,
                    p.[Name] AS ProductName,
                    du.IMEI1,
                    du.IMEI2,
                    du.SerialNumber,
                    du.WarehouseId,
                    du.CurrentStatus,
                    du.CostPrice,
                    p.Price AS SellingPrice,
                    du.CreatedDate
                FROM dbo.DeviceUnit du
                JOIN dbo.Product p ON du.ProductId = p.ProductId
                WHERE du.ProductId = @ProductId AND du.CurrentStatus = 1
                ORDER BY du.DeviceUnitId ASC;", connection);

            cmd.Parameters.AddWithValue("@ProductId", productId);

            using var reader = await cmd.ExecuteReaderAsync();
            return reader.DataReaderMapToList<DeviceUnit>();
        }

        public async Task<bool> UpdateDeviceStatus(long deviceUnitId, byte status, string? notes = null)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var cmd = new SqlCommand(@"
                UPDATE dbo.DeviceUnit
                SET CurrentStatus = @Status,
                    Notes = COALESCE(@Notes, Notes)
                WHERE DeviceUnitId = @Id;", connection);

            cmd.Parameters.AddWithValue("@Id", deviceUnitId);
            cmd.Parameters.AddWithValue("@Status", status);
            cmd.Parameters.AddWithValue("@Notes", (object?)notes ?? DBNull.Value);

            int rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }

        public async Task<int> GetInStockDeviceCount()
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var cmd = new SqlCommand("SELECT COUNT(1) FROM dbo.DeviceUnit WHERE CurrentStatus = 1;", connection);
            var res = await cmd.ExecuteScalarAsync();
            return res != null ? Convert.ToInt32(res) : 0;
        }

        public async Task<decimal> GetTotalStockValue()
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var cmd = new SqlCommand(@"
                SELECT ISNULL(SUM(ISNULL(du.CostPrice, CAST(p.Price AS DECIMAL(18,2)))), 0)
                FROM dbo.DeviceUnit du
                JOIN dbo.Product p ON du.ProductId = p.ProductId
                WHERE du.CurrentStatus = 1;", connection);

            var res = await cmd.ExecuteScalarAsync();
            return res != null && res != DBNull.Value ? Convert.ToDecimal(res) : 0;
        }
    }
}
