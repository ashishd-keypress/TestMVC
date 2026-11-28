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

        public async Task<List<SalesInvoice>> GetInvoices(string? search = null, int? userId = null)
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
                    s.CreatedDate,
                    s.UserId,
                    s.CreatedBy
                FROM dbo.SalesInvoice s
                LEFT JOIN dbo.Customer c ON s.CustomerId = c.CustomerId
                WHERE (@UserId IS NULL OR ISNULL(s.UserId, ISNULL(s.CreatedBy, 1)) = @UserId)
                  AND (@Search IS NULL 
                       OR s.InvoiceNumber LIKE '%' + @Search + '%' 
                       OR c.CustomerName LIKE '%' + @Search + '%' 
                       OR c.Phone LIKE '%' + @Search + '%')
                ORDER BY s.SalesInvoiceId DESC;", connection);

            cmd.Parameters.AddWithValue("@UserId", (object?)userId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Search", (object?)search ?? DBNull.Value);

            using var reader = await cmd.ExecuteReaderAsync();
            return reader.DataReaderMapToList<SalesInvoice>();
        }

        public async Task<SalesInvoice?> GetInvoiceById(long invoiceId, int? userId = null)
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
                    s.UserId,
                    s.CreatedBy,
                    ISNULL(sp.PaymentMethodId, 1) AS PaymentMethodId,
                    ISNULL(pm.MethodName, 'Cash') AS PaymentMethodName,
                    sp.TransactionReference
                FROM dbo.SalesInvoice s
                LEFT JOIN dbo.Customer c ON s.CustomerId = c.CustomerId
                LEFT JOIN dbo.SalesPayment sp ON s.SalesInvoiceId = sp.SalesInvoiceId
                LEFT JOIN dbo.PaymentMethod pm ON sp.PaymentMethodId = pm.PaymentMethodId
                WHERE s.SalesInvoiceId = @Id
                  AND (@UserId IS NULL OR ISNULL(s.UserId, ISNULL(s.CreatedBy, 1)) = @UserId);", connection))
            {
                cmd.Parameters.AddWithValue("@Id", invoiceId);
                cmd.Parameters.AddWithValue("@UserId", (object?)userId ?? DBNull.Value);
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
                        SubTotal, Discount, TaxAmount, GrandTotal, PaidAmount, DueAmount, Status, Notes, CreatedDate,
                        UserId, CreatedBy
                    )
                    VALUES (
                        @BranchId, @CustomerId, @InvoiceNumber, GETDATE(),
                        @SubTotal, @Discount, @TaxAmount, @GrandTotal, @PaidAmount, @DueAmount, @Status, @Notes, GETDATE(),
                        @UserId, @CreatedBy
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
                headerCmd.Parameters.AddWithValue("@UserId", (object?)invoice.UserId ?? (invoice.CreatedBy.HasValue ? invoice.CreatedBy.Value : 1));
                headerCmd.Parameters.AddWithValue("@CreatedBy", (object?)invoice.CreatedBy ?? (invoice.UserId.HasValue ? invoice.UserId.Value : 1));

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
                    int paymentMethodId = invoice.PaymentMethodId;
                    if (paymentMethodId <= 0)
                    {
                        using var pmLookupCmd = new SqlCommand(@"
                            SELECT TOP 1 PaymentMethodId
                            FROM dbo.PaymentMethod
                            WHERE ISNULL(IsDelete, 0) = 0
                              AND IsActive = 1
                              AND ISNULL(UserId, 1) = @UserId
                            ORDER BY PaymentMethodId ASC;", connection, transaction);
                        pmLookupCmd.Parameters.AddWithValue("@UserId", (object?)invoice.UserId ?? 1);
                        var pmObj = await pmLookupCmd.ExecuteScalarAsync();
                        paymentMethodId = pmObj != null && pmObj != DBNull.Value ? Convert.ToInt32(pmObj) : 1;
                    }

                    using var payCmd = new SqlCommand(@"
                        INSERT INTO dbo.SalesPayment (SalesInvoiceId, PaymentMethodId, Amount, TransactionReference, PaymentDate)
                        VALUES (@SalesInvoiceId, @PaymentMethodId, @Amount, @TransactionReference, GETDATE());", connection, transaction);

                    payCmd.Parameters.AddWithValue("@SalesInvoiceId", invoiceId);
                    payCmd.Parameters.AddWithValue("@PaymentMethodId", paymentMethodId);
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

        private static readonly string[] DefaultPaymentMethodNames =
        {
            "Cash",
            "UPI",
            "Google Pay",
            "PhonePe",
            "Paytm",
            "Credit/Debit Card",
            "Bank Transfer",
            "Customer Credit"
        };

        private static async Task EnsureUserPaymentMethodsSeededAsync(SqlConnection connection, int userId)
        {
            if (userId <= 0) return;

            using var checkCmd = new SqlCommand(@"SELECT COUNT(1) FROM dbo.PaymentMethod WHERE UserId = @UserId;", connection);
            checkCmd.Parameters.AddWithValue("@UserId", userId);
            int existingTotal = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());
            if (existingTotal > 0) return;

            foreach (var name in DefaultPaymentMethodNames)
            {
                using var seedCmd = new SqlCommand(@"
                    INSERT INTO dbo.PaymentMethod (MethodName, IsActive, IsDelete, UserId)
                    VALUES (@MethodName, 1, 0, @UserId);", connection);
                seedCmd.Parameters.AddWithValue("@MethodName", name);
                seedCmd.Parameters.AddWithValue("@UserId", userId);
                await seedCmd.ExecuteNonQueryAsync();
            }
        }

        public async Task<List<PaymentMethod>> GetPaymentMethods(int? userId = null)
        {
            int effectiveUserId = userId.HasValue && userId.Value > 0 ? userId.Value : 1;
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            await EnsureUserPaymentMethodsSeededAsync(connection, effectiveUserId);

            using var cmd = new SqlCommand(@"
                SELECT PaymentMethodId, MethodName, IsActive, UserId
                FROM dbo.PaymentMethod
                WHERE ISNULL(IsDelete, 0) = 0
                  AND IsActive = 1
                  AND ISNULL(UserId, 1) = @UserId
                ORDER BY PaymentMethodId ASC;", connection);
            cmd.Parameters.AddWithValue("@UserId", effectiveUserId);

            using var reader = await cmd.ExecuteReaderAsync();
            return reader.DataReaderMapToList<PaymentMethod>();
        }

        public async Task<PaymentMethod?> GetPaymentMethodById(int paymentMethodId, int? userId = null)
        {
            if (paymentMethodId <= 0) return null;

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var cmd = new SqlCommand(@"
                SELECT PaymentMethodId, MethodName, IsActive, UserId
                FROM dbo.PaymentMethod
                WHERE PaymentMethodId = @Id
                  AND ISNULL(IsDelete, 0) = 0
                  AND (@UserId IS NULL OR ISNULL(UserId, 1) = @UserId);", connection);
            cmd.Parameters.AddWithValue("@Id", paymentMethodId);
            cmd.Parameters.AddWithValue("@UserId", (object?)userId ?? DBNull.Value);

            using var reader = await cmd.ExecuteReaderAsync();
            return reader.DataReaderMapToSingle<PaymentMethod>();
        }

        public async Task<(bool Success, string Message, PaymentMethod? PaymentMethod)> SavePaymentMethod(PaymentMethod paymentMethod)
        {
            string trimmedName = (paymentMethod.MethodName ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(trimmedName))
            {
                return (false, "Payment Mode name is required.", null);
            }
            if (trimmedName.Length > 100)
            {
                return (false, "Payment Mode name cannot exceed 100 characters.", null);
            }

            paymentMethod.MethodName = trimmedName;
            int ownerUserId = paymentMethod.UserId.HasValue && paymentMethod.UserId.Value > 0
                ? paymentMethod.UserId.Value
                : 1;

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            if (paymentMethod.PaymentMethodId > 0)
            {
                // Verify record exists and belongs to the current user before updating
                using var ownCmd = new SqlCommand(@"
                    SELECT COUNT(1)
                    FROM dbo.PaymentMethod
                    WHERE PaymentMethodId = @Id
                      AND ISNULL(IsDelete, 0) = 0
                      AND (@UserId IS NULL OR ISNULL(UserId, 1) = @UserId);", connection);
                ownCmd.Parameters.AddWithValue("@Id", paymentMethod.PaymentMethodId);
                ownCmd.Parameters.AddWithValue("@UserId", (object?)paymentMethod.UserId ?? DBNull.Value);
                if (Convert.ToInt32(await ownCmd.ExecuteScalarAsync()) == 0)
                {
                    return (false, "Payment mode not found.", null);
                }
            }

            using (var dupCmd = new SqlCommand(@"
                SELECT COUNT(1)
                FROM dbo.PaymentMethod
                WHERE LOWER(LTRIM(RTRIM(MethodName))) = LOWER(@Name)
                  AND ISNULL(IsDelete, 0) = 0
                  AND ISNULL(UserId, 1) = @UserId
                  AND PaymentMethodId <> @Id;", connection))
            {
                dupCmd.Parameters.AddWithValue("@Name", trimmedName);
                dupCmd.Parameters.AddWithValue("@UserId", ownerUserId);
                dupCmd.Parameters.AddWithValue("@Id", paymentMethod.PaymentMethodId);
                int dupCount = Convert.ToInt32(await dupCmd.ExecuteScalarAsync());
                if (dupCount > 0)
                {
                    return (false, $"Payment mode '{trimmedName}' already exists.", null);
                }
            }

            if (paymentMethod.PaymentMethodId <= 0)
            {
                using var insertCmd = new SqlCommand(@"
                    INSERT INTO dbo.PaymentMethod (MethodName, IsActive, IsDelete, UserId)
                    VALUES (@MethodName, @IsActive, 0, @UserId);
                    SELECT CAST(SCOPE_IDENTITY() AS INT);", connection);
                insertCmd.Parameters.AddWithValue("@MethodName", trimmedName);
                insertCmd.Parameters.AddWithValue("@IsActive", paymentMethod.IsActive ? 1 : 0);
                insertCmd.Parameters.AddWithValue("@UserId", ownerUserId);

                var newIdObj = await insertCmd.ExecuteScalarAsync();
                int newId = newIdObj != null ? Convert.ToInt32(newIdObj) : 0;
                if (newId <= 0)
                {
                    return (false, "Unable to add payment mode.", null);
                }

                paymentMethod.PaymentMethodId = newId;
                paymentMethod.UserId = ownerUserId;
                return (true, "Payment mode added successfully.", paymentMethod);
            }
            else
            {
                using var updateCmd = new SqlCommand(@"
                    UPDATE dbo.PaymentMethod
                    SET MethodName = @MethodName,
                        IsActive = @IsActive
                    WHERE PaymentMethodId = @Id
                      AND ISNULL(IsDelete, 0) = 0
                      AND (@UserId IS NULL OR ISNULL(UserId, 1) = @UserId);", connection);
                updateCmd.Parameters.AddWithValue("@Id", paymentMethod.PaymentMethodId);
                updateCmd.Parameters.AddWithValue("@MethodName", trimmedName);
                updateCmd.Parameters.AddWithValue("@IsActive", paymentMethod.IsActive ? 1 : 0);
                updateCmd.Parameters.AddWithValue("@UserId", (object?)paymentMethod.UserId ?? DBNull.Value);

                int affected = await updateCmd.ExecuteNonQueryAsync();
                if (affected <= 0)
                {
                    return (false, "Payment mode not found.", null);
                }

                paymentMethod.UserId = ownerUserId;
                return (true, "Payment mode updated successfully.", paymentMethod);
            }
        }

        public async Task<(bool Success, string Message)> DeletePaymentMethod(int paymentMethodId, int? userId = null)
        {
            if (paymentMethodId <= 0)
            {
                return (false, "Invalid payment mode ID.");
            }

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            int ownerUserId;
            using (var existsCmd = new SqlCommand(@"
                SELECT ISNULL(UserId, 1)
                FROM dbo.PaymentMethod
                WHERE PaymentMethodId = @Id
                  AND ISNULL(IsDelete, 0) = 0
                  AND (@UserId IS NULL OR ISNULL(UserId, 1) = @UserId);", connection))
            {
                existsCmd.Parameters.AddWithValue("@Id", paymentMethodId);
                existsCmd.Parameters.AddWithValue("@UserId", (object?)userId ?? DBNull.Value);
                var ownerObj = await existsCmd.ExecuteScalarAsync();
                if (ownerObj == null || ownerObj == DBNull.Value)
                {
                    return (false, "Payment mode not found.");
                }
                ownerUserId = userId.HasValue && userId.Value > 0 ? userId.Value : Convert.ToInt32(ownerObj);
            }

            // Check usage strictly for the currently logged-in user's records only
            using (var refCmd = new SqlCommand(@"
                SELECT
                    (SELECT COUNT(1)
                     FROM dbo.Expense e
                     WHERE e.PaymentMethodId = @Id
                       AND ISNULL(e.UserId, ISNULL(e.CreatedBy, 1)) = @OwnerUserId) +
                    (SELECT COUNT(1)
                     FROM dbo.SalesPayment sp
                     LEFT JOIN dbo.SalesInvoice si ON sp.SalesInvoiceId = si.SalesInvoiceId
                     WHERE sp.PaymentMethodId = @Id
                       AND ISNULL(si.UserId, ISNULL(si.CreatedBy, 1)) = @OwnerUserId) +
                    (SELECT COUNT(1)
                     FROM dbo.SalesReturnPayment srp
                     LEFT JOIN dbo.SalesReturn sr ON srp.SalesReturnId = sr.SalesReturnId
                     LEFT JOIN dbo.SalesInvoice si ON sr.SalesInvoiceId = si.SalesInvoiceId
                     WHERE srp.PaymentMethodId = @Id
                       AND ISNULL(srp.CreatedBy, ISNULL(sr.CreatedBy, ISNULL(si.UserId, 1))) = @OwnerUserId) +
                    (SELECT COUNT(1)
                     FROM dbo.PurchasePayment pp
                     LEFT JOIN dbo.Purchase p ON pp.PurchaseId = p.PurchaseId
                     WHERE pp.PaymentMethodId = @Id
                       AND ISNULL(p.UserId, ISNULL(p.CreatedBy, 1)) = @OwnerUserId);", connection))
            {
                refCmd.Parameters.AddWithValue("@Id", paymentMethodId);
                refCmd.Parameters.AddWithValue("@OwnerUserId", ownerUserId);
                int usedCount = Convert.ToInt32(await refCmd.ExecuteScalarAsync());
                if (usedCount > 0)
                {
                    return (false, $"Cannot delete this payment mode because it is used by {usedCount} existing record(s).");
                }
            }

            using var deleteCmd = new SqlCommand(@"
                UPDATE dbo.PaymentMethod
                SET IsDelete = 1,
                    IsActive = 0
                WHERE PaymentMethodId = @Id
                  AND ISNULL(UserId, 1) = @OwnerUserId
                  AND ISNULL(IsDelete, 0) = 0;", connection);
            deleteCmd.Parameters.AddWithValue("@Id", paymentMethodId);
            deleteCmd.Parameters.AddWithValue("@OwnerUserId", ownerUserId);
            int affected = await deleteCmd.ExecuteNonQueryAsync();
            return affected > 0
                ? (true, "Payment mode deleted successfully.")
                : (false, "Unable to delete payment mode.");
        }

        public async Task<decimal> GetTodaySalesAmount(int? userId = null)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var cmd = new SqlCommand(@"
                SELECT ISNULL(SUM(GrandTotal), 0)
                FROM dbo.SalesInvoice s
                WHERE CAST(InvoiceDate AS DATE) = CAST(GETDATE() AS DATE)
                  AND Status != 'Cancelled'
                  AND (@UserId IS NULL OR ISNULL(s.UserId, ISNULL(s.CreatedBy, 1)) = @UserId);", connection);

            cmd.Parameters.AddWithValue("@UserId", (object?)userId ?? DBNull.Value);

            var res = await cmd.ExecuteScalarAsync();
            return res != null && res != DBNull.Value ? Convert.ToDecimal(res) : 0;
        }

        public async Task<List<SalesInvoice>> GetRecentInvoices(int count = 5, int? userId = null)
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
                WHERE (@UserId IS NULL OR ISNULL(s.UserId, ISNULL(s.CreatedBy, 1)) = @UserId)
                ORDER BY s.SalesInvoiceId DESC;", connection);

            cmd.Parameters.AddWithValue("@Count", count);
            cmd.Parameters.AddWithValue("@UserId", (object?)userId ?? DBNull.Value);

            using var reader = await cmd.ExecuteReaderAsync();
            return reader.DataReaderMapToList<SalesInvoice>();
        }
    }
}
