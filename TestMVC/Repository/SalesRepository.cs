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
    public class SalesRepository : ISalesRepository
    {
        private readonly string _connectionString;

        public SalesRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("DefaultConnection missing");
        }

        public async Task<List<SalesInvoice>> GetInvoices(string? search = null)
        {
            var list = new List<SalesInvoice>();
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var cmd = new SqlCommand(@"
                SELECT 
                    s.SalesInvoiceId,
                    s.BranchId,
                    s.CustomerId,
                    ISNULL(c.CustomerName, 'Walk-in Customer') AS CustomerName,
                    ISNULL(c.Phone, '-') AS CustomerPhone,
                    s.InvoiceNumber,
                    s.InvoiceDate,
                    s.SubTotal,
                    s.Discount,
                    s.TaxAmount,
                    s.GrandTotal,
                    s.PaidAmount,
                    s.DueAmount,
                    s.Status,
                    s.Notes,
                    s.CreatedDate
                FROM dbo.SalesInvoice s
                LEFT JOIN dbo.Customer c ON s.CustomerId = c.CustomerId
                WHERE (@Search IS NULL 
                       OR s.InvoiceNumber LIKE '%' + @Search + '%' 
                       OR c.CustomerName LIKE '%' + @Search + '%' 
                       OR c.Phone LIKE '%' + @Search + '%')
                ORDER BY s.SalesInvoiceId DESC;", connection);

            cmd.Parameters.AddWithValue("@Search", (object?)search ?? DBNull.Value);

            using var reader = await cmd.ExecuteReaderAsync();
            return reader.DataReaderMapToList<SalesInvoice>();
        }

        public async Task<SalesInvoice?> GetInvoiceById(long invoiceId)
        {
            SalesInvoice? invoice = null;
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using (var cmd = new SqlCommand(@"
                SELECT 
                    s.SalesInvoiceId,
                    s.BranchId,
                    s.CustomerId,
                    ISNULL(c.CustomerName, 'Walk-in Customer') AS CustomerName,
                    ISNULL(c.Phone, '-') AS CustomerPhone,
                    c.Address AS CustomerAddress,
                    c.GSTNumber AS CustomerGST,
                    s.InvoiceNumber,
                    s.InvoiceDate,
                    s.SubTotal,
                    s.Discount,
                    s.TaxAmount,
                    s.GrandTotal,
                    s.PaidAmount,
                    s.DueAmount,
                    s.Status,
                    s.Notes,
                    s.CreatedDate,
                    ISNULL(sp.PaymentMethodId, 1) AS PaymentMethodId,
                    ISNULL(pm.MethodName, 'Cash') AS PaymentMethodName,
                    sp.TransactionReference
                FROM dbo.SalesInvoice s
                LEFT JOIN dbo.Customer c ON s.CustomerId = c.CustomerId
                LEFT JOIN dbo.SalesPayment sp ON s.SalesInvoiceId = sp.SalesInvoiceId
                LEFT JOIN dbo.PaymentMethod pm ON sp.PaymentMethodId = pm.PaymentMethodId
                WHERE s.SalesInvoiceId = @Id;", connection))
            {
                cmd.Parameters.AddWithValue("@Id", invoiceId);
                using var reader = await cmd.ExecuteReaderAsync();
                invoice = reader.DataReaderMapToSingle<SalesInvoice>();
            }

            if (invoice != null)
            {
                using var itemCmd = new SqlCommand(@"
                    SELECT 
                        sii.SalesInvoiceItemId,
                        sii.SalesInvoiceId,
                        sii.ProductId,
                        p.[Name] AS ProductName,
                        p.RAM,
                        p.Storage,
                        p.Color,
                        ISNULL(p.TrackIMEI, 0) AS TrackIMEI,
                        sii.Quantity,
                        sii.UnitPrice,
                        sii.Discount,
                        sii.TaxAmount,
                        sii.TotalAmount,
                        sis.DeviceUnitId,
                        du.IMEI1,
                        du.IMEI2
                    FROM dbo.SalesInvoiceItem sii
                    JOIN dbo.Product p ON sii.ProductId = p.ProductId
                    LEFT JOIN dbo.SalesItemSerial sis ON sii.SalesInvoiceItemId = sis.SalesInvoiceItemId
                    LEFT JOIN dbo.DeviceUnit du ON sis.DeviceUnitId = du.DeviceUnitId
                    WHERE sii.SalesInvoiceId = @InvoiceId;", connection);

                itemCmd.Parameters.AddWithValue("@InvoiceId", invoiceId);
                using var itemReader = await itemCmd.ExecuteReaderAsync();
                invoice.Items = itemReader.DataReaderMapToList<SalesInvoiceItem>();
            }

            return invoice;
        }

        public async Task<long> CreateSalesInvoice(SalesInvoice invoice)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                // Generate Invoice Number if not set
                if (string.IsNullOrWhiteSpace(invoice.InvoiceNumber))
                {
                    invoice.InvoiceNumber = $"INV-{DateTime.Now:yyyyMMdd}-{new Random().Next(1000, 9999)}";
                }

                // 1. Insert Sales Invoice
                using var headerCmd = new SqlCommand(@"
                    INSERT INTO dbo.SalesInvoice (
                        BranchId, CustomerId, InvoiceNumber, InvoiceDate,
                        SubTotal, Discount, TaxAmount, GrandTotal, PaidAmount, DueAmount, Status, Notes, CreatedDate
                    )
                    VALUES (
                        @BranchId, @CustomerId, @InvoiceNumber, GETDATE(),
                        @SubTotal, @Discount, @TaxAmount, @GrandTotal, @PaidAmount, @DueAmount, @Status, @Notes, GETDATE()
                    );
                    SELECT SCOPE_IDENTITY();", connection, transaction);

                headerCmd.Parameters.AddWithValue("@BranchId", invoice.BranchId > 0 ? invoice.BranchId : 1);
                headerCmd.Parameters.AddWithValue("@CustomerId", (object?)invoice.CustomerId ?? DBNull.Value);
                headerCmd.Parameters.AddWithValue("@InvoiceNumber", invoice.InvoiceNumber);
                headerCmd.Parameters.AddWithValue("@SubTotal", invoice.SubTotal);
                headerCmd.Parameters.AddWithValue("@Discount", invoice.Discount);
                headerCmd.Parameters.AddWithValue("@TaxAmount", invoice.TaxAmount);
                headerCmd.Parameters.AddWithValue("@GrandTotal", invoice.GrandTotal);
                headerCmd.Parameters.AddWithValue("@PaidAmount", invoice.PaidAmount);
                headerCmd.Parameters.AddWithValue("@DueAmount", invoice.DueAmount);
                headerCmd.Parameters.AddWithValue("@Status", invoice.Status ?? "Completed");
                headerCmd.Parameters.AddWithValue("@Notes", (object?)invoice.Notes ?? DBNull.Value);

                var invIdObj = await headerCmd.ExecuteScalarAsync();
                long invoiceId = Convert.ToInt64(invIdObj);
                invoice.SalesInvoiceId = invoiceId;

                // 2. Insert Invoice Items and Update Inventory
                foreach (var item in invoice.Items)
                {
                    using var itemCmd = new SqlCommand(@"
                        INSERT INTO dbo.SalesInvoiceItem (SalesInvoiceId, ProductId, Quantity, UnitPrice, Discount, TaxAmount, TotalAmount)
                        VALUES (@SalesInvoiceId, @ProductId, @Quantity, @UnitPrice, @Discount, @TaxAmount, @TotalAmount);
                        SELECT SCOPE_IDENTITY();", connection, transaction);

                    itemCmd.Parameters.AddWithValue("@SalesInvoiceId", invoiceId);
                    itemCmd.Parameters.AddWithValue("@ProductId", item.ProductId);
                    itemCmd.Parameters.AddWithValue("@Quantity", item.Quantity);
                    itemCmd.Parameters.AddWithValue("@UnitPrice", item.UnitPrice);
                    itemCmd.Parameters.AddWithValue("@Discount", item.Discount);
                    itemCmd.Parameters.AddWithValue("@TaxAmount", item.TaxAmount);
                    itemCmd.Parameters.AddWithValue("@TotalAmount", item.TotalAmount);

                    var itemIdObj = await itemCmd.ExecuteScalarAsync();
                    long invoiceItemId = Convert.ToInt64(itemIdObj);

                    // Decrement StockBalance
                    using var stockCmd = new SqlCommand(@"
                        UPDATE dbo.StockBalance
                        SET QuantityOnHand = CASE WHEN QuantityOnHand >= @Qty THEN QuantityOnHand - @Qty ELSE 0 END
                        WHERE ProductId = @ProductId AND WarehouseId = 1;", connection, transaction);

                    stockCmd.Parameters.AddWithValue("@Qty", item.Quantity);
                    stockCmd.Parameters.AddWithValue("@ProductId", item.ProductId);
                    await stockCmd.ExecuteNonQueryAsync();

                    // If DeviceUnitId is provided (tracked mobile phone)
                    if (item.DeviceUnitId.HasValue && item.DeviceUnitId.Value > 0)
                    {
                        // Link Serial to Invoice Item
                        using var serialCmd = new SqlCommand(@"
                            INSERT INTO dbo.SalesItemSerial (SalesInvoiceItemId, DeviceUnitId, IsReturned)
                            VALUES (@SalesInvoiceItemId, @DeviceUnitId, 0);", connection, transaction);

                        serialCmd.Parameters.AddWithValue("@SalesInvoiceItemId", invoiceItemId);
                        serialCmd.Parameters.AddWithValue("@DeviceUnitId", item.DeviceUnitId.Value);
                        await serialCmd.ExecuteNonQueryAsync();

                        // Mark DeviceUnit as Sold (Status = 3)
                        using var devCmd = new SqlCommand(@"
                            UPDATE dbo.DeviceUnit
                            SET CurrentStatus = 3,
                                WarrantyStartDate = GETDATE(),
                                WarrantyEndDate = DATEADD(MONTH, 12, GETDATE())
                            WHERE DeviceUnitId = @DeviceUnitId;", connection, transaction);

                        devCmd.Parameters.AddWithValue("@DeviceUnitId", item.DeviceUnitId.Value);
                        await devCmd.ExecuteNonQueryAsync();
                    }
                }

                // 3. Record Payment
                if (invoice.PaidAmount > 0)
                {
                    using var payCmd = new SqlCommand(@"
                        INSERT INTO dbo.SalesPayment (SalesInvoiceId, PaymentMethodId, Amount, TransactionReference, PaymentDate)
                        VALUES (@SalesInvoiceId, @PaymentMethodId, @Amount, @TransactionReference, GETDATE());", connection, transaction);

                    payCmd.Parameters.AddWithValue("@SalesInvoiceId", invoiceId);
                    payCmd.Parameters.AddWithValue("@PaymentMethodId", invoice.PaymentMethodId > 0 ? invoice.PaymentMethodId : 1);
                    payCmd.Parameters.AddWithValue("@Amount", invoice.PaidAmount);
                    payCmd.Parameters.AddWithValue("@TransactionReference", (object?)invoice.TransactionReference ?? DBNull.Value);
                    await payCmd.ExecuteNonQueryAsync();
                }

                // 4. Update Customer Balance if due amount > 0
                if (invoice.CustomerId.HasValue && invoice.CustomerId.Value > 0 && invoice.DueAmount > 0)
                {
                    using var custCmd = new SqlCommand(@"
                        UPDATE dbo.Customer
                        SET CurrentBalance = CurrentBalance + @Due
                        WHERE CustomerId = @CustomerId;", connection, transaction);

                    custCmd.Parameters.AddWithValue("@Due", invoice.DueAmount);
                    custCmd.Parameters.AddWithValue("@CustomerId", invoice.CustomerId.Value);
                    await custCmd.ExecuteNonQueryAsync();
                }

                transaction.Commit();
                return invoiceId;
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task<List<PaymentMethod>> GetPaymentMethods()
        {
            var list = new List<PaymentMethod>();
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var cmd = new SqlCommand(@"
                SELECT PaymentMethodId, MethodName, IsActive 
                FROM dbo.PaymentMethod 
                WHERE IsActive = 1 
                ORDER BY PaymentMethodId ASC;", connection);

            using var reader = await cmd.ExecuteReaderAsync();
            return reader.DataReaderMapToList<PaymentMethod>();
        }

        public async Task<decimal> GetTodaySalesAmount()
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var cmd = new SqlCommand(@"
                SELECT ISNULL(SUM(GrandTotal), 0)
                FROM dbo.SalesInvoice
                WHERE CAST(InvoiceDate AS DATE) = CAST(GETDATE() AS DATE)
                  AND Status != 'Cancelled';", connection);

            var res = await cmd.ExecuteScalarAsync();
            return res != null && res != DBNull.Value ? Convert.ToDecimal(res) : 0;
        }

        public async Task<List<SalesInvoice>> GetRecentInvoices(int count = 5)
        {
            var list = new List<SalesInvoice>();
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var cmd = new SqlCommand(@"
                SELECT TOP (@Count)
                    s.SalesInvoiceId,
                    s.InvoiceNumber,
                    s.InvoiceDate,
                    s.GrandTotal,
                    s.PaidAmount,
                    s.DueAmount,
                    s.Status,
                    ISNULL(c.CustomerName, 'Walk-in Customer') AS CustomerName,
                    ISNULL(c.Phone, '-') AS CustomerPhone
                FROM dbo.SalesInvoice s
                LEFT JOIN dbo.Customer c ON s.CustomerId = c.CustomerId
                ORDER BY s.SalesInvoiceId DESC;", connection);

            cmd.Parameters.AddWithValue("@Count", count);

            using var reader = await cmd.ExecuteReaderAsync();
            return reader.DataReaderMapToList<SalesInvoice>();
        }
    }
}
