using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using TestMVC.Interface;
using TestMVC.Models;
using Utility;

namespace TestMVC.Repository
{
    public class PurchaseRepository : IPurchaseRepository
    {
        private readonly string _connectionString;

        public PurchaseRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("DefaultConnection missing");
        }

        public async Task<List<Purchase>> GetPurchases(string? search = null)
        {
            var list = new List<Purchase>();
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var cmd = new SqlCommand(@"
                SELECT 
                    p.PurchaseId,
                    p.SupplierId,
                    s.SupplierName,
                    s.Phone AS SupplierPhone,
                    p.BranchId,
                    p.InvoiceNumber,
                    p.PurchaseDate,
                    p.SubTotal,
                    p.Discount,
                    p.TaxAmount,
                    p.GrandTotal,
                    p.PaidAmount,
                    p.DueAmount,
                    p.Status,
                    p.Notes,
                    p.CreatedDate
                FROM dbo.Purchase p
                LEFT JOIN dbo.Supplier s ON p.SupplierId = s.SupplierId
                WHERE (@Search IS NULL 
                       OR p.InvoiceNumber LIKE '%' + @Search + '%' 
                       OR s.SupplierName LIKE '%' + @Search + '%'
                       OR s.Phone LIKE '%' + @Search + '%')
                ORDER BY p.PurchaseId DESC;", connection);

            cmd.Parameters.AddWithValue("@Search", (object?)search ?? DBNull.Value);

            using var reader = await cmd.ExecuteReaderAsync();
            return reader.DataReaderMapToList<Purchase>();
        }

        public async Task<Purchase?> GetPurchaseById(long purchaseId)
        {
            Purchase? purchase = null;
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using (var cmd = new SqlCommand(@"
                SELECT 
                    p.PurchaseId,
                    p.SupplierId,
                    s.SupplierName,
                    s.Phone AS SupplierPhone,
                    p.BranchId,
                    p.InvoiceNumber,
                    p.PurchaseDate,
                    p.SubTotal,
                    p.Discount,
                    p.TaxAmount,
                    p.GrandTotal,
                    p.PaidAmount,
                    p.DueAmount,
                    p.Status,
                    p.Notes,
                    p.CreatedDate
                FROM dbo.Purchase p
                LEFT JOIN dbo.Supplier s ON p.SupplierId = s.SupplierId
                WHERE p.PurchaseId = @Id;", connection))
            {
                cmd.Parameters.AddWithValue("@Id", purchaseId);
                using var reader = await cmd.ExecuteReaderAsync();
                purchase = reader.DataReaderMapToSingle<Purchase>();
            }

            if (purchase != null)
            {
                using var itemCmd = new SqlCommand(@"
                    SELECT 
                        pi.PurchaseItemId,
                        pi.PurchaseId,
                        pi.ProductId,
                        pr.[Name] AS ProductName,
                        ISNULL(pr.TrackIMEI, 0) AS TrackIMEI,
                        pi.Quantity,
                        pi.UnitCost,
                        pi.Discount,
                        pi.TaxAmount,
                        pi.TotalAmount
                    FROM dbo.PurchaseItem pi
                    JOIN dbo.Product pr ON pi.ProductId = pr.ProductId
                    WHERE pi.PurchaseId = @PurchaseId;", connection);

                itemCmd.Parameters.AddWithValue("@PurchaseId", purchaseId);
                using var itemReader = await itemCmd.ExecuteReaderAsync();
                purchase.Items = itemReader.DataReaderMapToList<PurchaseItem>();
            }

            return purchase;
        }

        public async Task<long> SavePurchase(Purchase purchase)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                // 1. Insert Purchase Header
                using var headerCmd = new SqlCommand(@"
                    INSERT INTO dbo.Purchase (
                        SupplierId, BranchId, InvoiceNumber, PurchaseDate, 
                        SubTotal, Discount, TaxAmount, GrandTotal, PaidAmount, DueAmount, Status, Notes, CreatedDate
                    )
                    VALUES (
                        @SupplierId, @BranchId, @InvoiceNumber, @PurchaseDate, 
                        @SubTotal, @Discount, @TaxAmount, @GrandTotal, @PaidAmount, @DueAmount, @Status, @Notes, GETDATE()
                    );
                    SELECT SCOPE_IDENTITY();", connection, transaction);

                headerCmd.Parameters.AddWithValue("@SupplierId", purchase.SupplierId);
                headerCmd.Parameters.AddWithValue("@BranchId", purchase.BranchId > 0 ? purchase.BranchId : 1);
                headerCmd.Parameters.AddWithValue("@InvoiceNumber", purchase.InvoiceNumber);
                headerCmd.Parameters.AddWithValue("@PurchaseDate", purchase.PurchaseDate);
                headerCmd.Parameters.AddWithValue("@SubTotal", purchase.SubTotal);
                headerCmd.Parameters.AddWithValue("@Discount", purchase.Discount);
                headerCmd.Parameters.AddWithValue("@TaxAmount", purchase.TaxAmount);
                headerCmd.Parameters.AddWithValue("@GrandTotal", purchase.GrandTotal);
                headerCmd.Parameters.AddWithValue("@PaidAmount", purchase.PaidAmount);
                headerCmd.Parameters.AddWithValue("@DueAmount", purchase.DueAmount);
                headerCmd.Parameters.AddWithValue("@Status", purchase.Status ?? "Received");
                headerCmd.Parameters.AddWithValue("@Notes", (object?)purchase.Notes ?? DBNull.Value);

                var purchaseIdObj = await headerCmd.ExecuteScalarAsync();
                long purchaseId = Convert.ToInt64(purchaseIdObj);
                purchase.PurchaseId = purchaseId;

                // 2. Insert Items, Update StockBalance, and Create DeviceUnits
                foreach (var item in purchase.Items)
                {
                    using var itemCmd = new SqlCommand(@"
                        INSERT INTO dbo.PurchaseItem (PurchaseId, ProductId, Quantity, UnitCost, Discount, TaxAmount, TotalAmount)
                        VALUES (@PurchaseId, @ProductId, @Quantity, @UnitCost, @Discount, @TaxAmount, @TotalAmount);
                        SELECT SCOPE_IDENTITY();", connection, transaction);

                    itemCmd.Parameters.AddWithValue("@PurchaseId", purchaseId);
                    itemCmd.Parameters.AddWithValue("@ProductId", item.ProductId);
                    itemCmd.Parameters.AddWithValue("@Quantity", item.Quantity);
                    itemCmd.Parameters.AddWithValue("@UnitCost", item.UnitCost);
                    itemCmd.Parameters.AddWithValue("@Discount", item.Discount);
                    itemCmd.Parameters.AddWithValue("@TaxAmount", item.TaxAmount);
                    itemCmd.Parameters.AddWithValue("@TotalAmount", item.TotalAmount);

                    var purchaseItemIdObj = await itemCmd.ExecuteScalarAsync();
                    long purchaseItemId = Convert.ToInt64(purchaseItemIdObj);

                    // Update or Insert StockBalance
                    using var stockCmd = new SqlCommand(@"
                        IF EXISTS (SELECT 1 FROM dbo.StockBalance WHERE ProductId = @ProductId AND WarehouseId = 1)
                        BEGIN
                            UPDATE dbo.StockBalance
                            SET QuantityOnHand = QuantityOnHand + @Qty
                            WHERE ProductId = @ProductId AND WarehouseId = 1;
                        END
                        ELSE
                        BEGIN
                            INSERT INTO dbo.StockBalance (ProductId, WarehouseId, QuantityOnHand, QuantityReserved, ReorderLevel)
                            VALUES (@ProductId, 1, @Qty, 0, 5);
                        END;", connection, transaction);

                    stockCmd.Parameters.AddWithValue("@ProductId", item.ProductId);
                    stockCmd.Parameters.AddWithValue("@Qty", item.Quantity);
                    await stockCmd.ExecuteNonQueryAsync();

                    // If IMEIs provided, parse and create DeviceUnit records
                    if (!string.IsNullOrWhiteSpace(item.IMEINumbers))
                    {
                        var imeiLines = item.IMEINumbers
                            .Split(new[] { '\r', '\n', ',' }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(s => s.Trim())
                            .Where(s => !string.IsNullOrEmpty(s))
                            .ToList();

                        foreach (var rawImei in imeiLines)
                        {
                            string imei1 = rawImei;
                            string? imei2 = null;

                            // If format is IMEI1/IMEI2 or IMEI1;IMEI2
                            if (rawImei.Contains('/') || rawImei.Contains(';'))
                            {
                                var parts = rawImei.Split(new[] { '/', ';' }, StringSplitOptions.RemoveEmptyEntries);
                                imei1 = parts[0].Trim();
                                if (parts.Length > 1) imei2 = parts[1].Trim();
                            }

                            using var deviceCmd = new SqlCommand(@"
                                IF NOT EXISTS (SELECT 1 FROM dbo.DeviceUnit WHERE IMEI1 = @IMEI1)
                                BEGIN
                                    INSERT INTO dbo.DeviceUnit (
                                        ProductId, PurchaseItemId, IMEI1, IMEI2, 
                                        WarehouseId, CurrentStatus, CostPrice, WarrantyStartDate, CreatedDate
                                    )
                                    VALUES (
                                        @ProductId, @PurchaseItemId, @IMEI1, @IMEI2, 
                                        1, 1, @CostPrice, GETDATE(), GETDATE()
                                    );
                                END;", connection, transaction);

                            deviceCmd.Parameters.AddWithValue("@ProductId", item.ProductId);
                            deviceCmd.Parameters.AddWithValue("@PurchaseItemId", purchaseItemId);
                            deviceCmd.Parameters.AddWithValue("@IMEI1", imei1);
                            deviceCmd.Parameters.AddWithValue("@IMEI2", (object?)imei2 ?? DBNull.Value);
                            deviceCmd.Parameters.AddWithValue("@CostPrice", item.UnitCost);

                            await deviceCmd.ExecuteNonQueryAsync();
                        }
                    }
                }

                // 3. Update Supplier Balance if due amount > 0
                if (purchase.DueAmount > 0)
                {
                    using var suppCmd = new SqlCommand(@"
                        UPDATE dbo.Supplier
                        SET CurrentBalance = CurrentBalance + @Due
                        WHERE SupplierId = @SupplierId;", connection, transaction);

                    suppCmd.Parameters.AddWithValue("@Due", purchase.DueAmount);
                    suppCmd.Parameters.AddWithValue("@SupplierId", purchase.SupplierId);
                    await suppCmd.ExecuteNonQueryAsync();
                }

                transaction.Commit();
                return purchaseId;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
    }
}
