SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

IF COL_LENGTH('dbo.DeviceUnit', 'CostPrice') IS NULL
    ALTER TABLE dbo.DeviceUnit ADD CostPrice DECIMAL(18,2) NULL;
GO

IF COL_LENGTH('dbo.DeviceUnit', 'Notes') IS NULL
    ALTER TABLE dbo.DeviceUnit ADD Notes NVARCHAR(500) NULL;
GO

UPDATE dbo.Product 
SET TrackIMEI = 1, RAM = '8 GB', Storage = '256 GB', Color = 'Ultramarine Blue', MRP = 89900.00, SellingPrice = 89900.00, WarrantyMonths = 12, SKU = 'APL-IP16-256-BLU', Barcode = '8901234567890'
WHERE ProductId = 1;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Supplier WHERE Phone = '9825012345')
BEGIN
    INSERT INTO dbo.Supplier (SupplierName, ContactPerson, Phone, Email, GSTNumber, Address, OpeningBalance, CurrentBalance, IsActive, CreatedDate)
    VALUES ('Redington India Ltd (Apple Distributor)', 'Suresh Patel', '9825012345', 'orders@redington.in', '24AAACR1234R1Z8', 'B-404, Tech Park, SG Highway, Ahmedabad', 0, 0, 1, GETDATE());
END;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Customer WHERE Phone = '9898011223')
BEGIN
    INSERT INTO dbo.Customer (CustomerName, Phone, Email, GSTNumber, Address, OpeningBalance, CurrentBalance, IsActive, CreatedDate)
    VALUES ('Vijay Sharma', '9898011223', 'vijay.sharma@example.com', '24AAAPS9876Q1Z2', '12, Shanti Kunj Society, Satellite, Ahmedabad', 0, 0, 1, GETDATE());
END;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.DeviceUnit WHERE ProductId = 1)
BEGIN
    INSERT INTO dbo.DeviceUnit (ProductId, IMEI1, IMEI2, SerialNumber, WarehouseId, CurrentStatus, CostPrice, WarrantyStartDate, CreatedDate)
    VALUES 
    (1, '354892110293841', '354892110293842', 'C39XYZ001APL', 1, 1, 78000.00, GETDATE(), GETDATE()),
    (1, '354892110293843', '354892110293844', 'C39XYZ002APL', 1, 1, 78000.00, GETDATE(), GETDATE()),
    (1, '354892110293845', '354892110293846', 'C39XYZ003APL', 1, 1, 78000.00, GETDATE(), GETDATE());

    IF EXISTS (SELECT 1 FROM dbo.StockBalance WHERE ProductId = 1 AND WarehouseId = 1)
    BEGIN
        UPDATE dbo.StockBalance SET QuantityOnHand = 3 WHERE ProductId = 1 AND WarehouseId = 1;
    END
    ELSE
    BEGIN
        INSERT INTO dbo.StockBalance (ProductId, WarehouseId, QuantityOnHand, QuantityReserved, ReorderLevel)
        VALUES (1, 1, 3, 0, 5);
    END;
END;
GO
