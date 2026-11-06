-- ============================================================================
-- MOBILE SHOP MANAGEMENT SYSTEM (MOBILE SELLER SOFTWARE)
-- Complete Database Architecture Script (9 Modules)
-- Architecture: Single Business with Multi-Branch & Warehouse Support
-- Engine: Microsoft SQL Server 2019+
-- ============================================================================

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

-- ============================================================================
-- MODULE 1: COMPANY, BRANCH & WAREHOUSE HIERARCHY
-- ============================================================================

-- 1.1 Company
IF OBJECT_ID('dbo.Company', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Company (
        CompanyId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Company PRIMARY KEY CLUSTERED,
        CompanyName NVARCHAR(150) NOT NULL,
        GSTNumber NVARCHAR(50) NULL,
        Address NVARCHAR(500) NULL,
        Phone NVARCHAR(30) NULL,
        Email NVARCHAR(100) NULL,
        LogoPath NVARCHAR(500) NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_Company_IsActive DEFAULT 1,
        CreatedDate DATETIME2 NOT NULL CONSTRAINT DF_Company_CreatedDate DEFAULT SYSUTCDATETIME()
    );
END
GO

-- 1.2 Branch
IF OBJECT_ID('dbo.Branch', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Branch (
        BranchId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Branch PRIMARY KEY CLUSTERED,
        CompanyId BIGINT NOT NULL CONSTRAINT FK_Branch_Company FOREIGN KEY REFERENCES dbo.Company(CompanyId),
        BranchName NVARCHAR(150) NOT NULL,
        BranchCode NVARCHAR(50) NULL,
        Address NVARCHAR(500) NULL,
        Phone NVARCHAR(30) NULL,
        Email NVARCHAR(100) NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_Branch_IsActive DEFAULT 1,
        CreatedDate DATETIME2 NOT NULL CONSTRAINT DF_Branch_CreatedDate DEFAULT SYSUTCDATETIME()
    );
END
GO

-- 1.3 Warehouse (Storage location per branch)
IF OBJECT_ID('dbo.Warehouse', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Warehouse (
        WarehouseId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Warehouse PRIMARY KEY CLUSTERED,
        BranchId BIGINT NOT NULL CONSTRAINT FK_Warehouse_Branch FOREIGN KEY REFERENCES dbo.Branch(BranchId),
        WarehouseName NVARCHAR(150) NOT NULL,
        Location NVARCHAR(250) NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_Warehouse_IsActive DEFAULT 1,
        CreatedDate DATETIME2 NOT NULL CONSTRAINT DF_Warehouse_CreatedDate DEFAULT SYSUTCDATETIME()
    );
END
GO

-- ============================================================================
-- MODULE 2: USER, ROLE & PERMISSION MANAGEMENT
-- ============================================================================

-- 2.1 Roles
IF OBJECT_ID('dbo.Role', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Role (
        RoleId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Role PRIMARY KEY CLUSTERED,
        RoleName NVARCHAR(100) NOT NULL,
        Description NVARCHAR(250) NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_Role_IsActive DEFAULT 1
    );
END
GO

-- 2.2 Permissions
IF OBJECT_ID('dbo.Permission', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Permission (
        PermissionId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Permission PRIMARY KEY CLUSTERED,
        PermissionName NVARCHAR(100) NOT NULL,
        ModuleName NVARCHAR(100) NOT NULL,
        Description NVARCHAR(250) NULL
    );
END
GO

-- 2.3 Role Permissions (Granular matrix)
IF OBJECT_ID('dbo.RolePermission', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.RolePermission (
        RolePermissionId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_RolePermission PRIMARY KEY CLUSTERED,
        RoleId BIGINT NOT NULL CONSTRAINT FK_RolePermission_Role FOREIGN KEY REFERENCES dbo.Role(RoleId),
        PermissionId BIGINT NOT NULL CONSTRAINT FK_RolePermission_Permission FOREIGN KEY REFERENCES dbo.Permission(PermissionId),
        CanView BIT NOT NULL CONSTRAINT DF_RolePermission_CanView DEFAULT 0,
        CanAdd BIT NOT NULL CONSTRAINT DF_RolePermission_CanAdd DEFAULT 0,
        CanEdit BIT NOT NULL CONSTRAINT DF_RolePermission_CanEdit DEFAULT 0,
        CanDelete BIT NOT NULL CONSTRAINT DF_RolePermission_CanDelete DEFAULT 0
    );
END
GO

-- 2.4 User Roles Junction
IF OBJECT_ID('dbo.UserRole', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.UserRole (
        UserRoleId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_UserRole PRIMARY KEY CLUSTERED,
        UserId INT NOT NULL,
        RoleId BIGINT NOT NULL CONSTRAINT FK_UserRole_Role FOREIGN KEY REFERENCES dbo.Role(RoleId)
    );
END
GO

-- ============================================================================
-- MODULE 3: PRODUCT MANAGEMENT (MODELS, VARIANTS & SPECIFICATIONS)
-- ============================================================================

-- 3.1 Product Models (e.g. iPhone 17, Samsung Galaxy S25)
IF OBJECT_ID('dbo.ProductModel', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ProductModel (
        ProductModelId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ProductModel PRIMARY KEY CLUSTERED,
        BrandId INT NOT NULL,
        ModelName NVARCHAR(150) NOT NULL,
        ModelCode NVARCHAR(100) NULL,
        Description NVARCHAR(MAX) NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_ProductModel_IsActive DEFAULT 1,
        CreatedDate DATETIME2 NOT NULL CONSTRAINT DF_ProductModel_CreatedDate DEFAULT SYSUTCDATETIME()
    );
END
GO

-- 3.2 Product Specifications (e.g. Processor, Display, Battery, Camera)
IF OBJECT_ID('dbo.ProductSpecification', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ProductSpecification (
        SpecificationId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ProductSpecification PRIMARY KEY CLUSTERED,
        ProductModelId BIGINT NOT NULL CONSTRAINT FK_ProductSpecification_Model FOREIGN KEY REFERENCES dbo.ProductModel(ProductModelId),
        SpecificationName NVARCHAR(100) NOT NULL,
        SpecificationValue NVARCHAR(500) NOT NULL
    );
END
GO

-- Evolve existing dbo.Product table with Mobile Shop variant fields
IF COL_LENGTH('dbo.Product', 'ProductModelId') IS NULL
    ALTER TABLE dbo.Product ADD ProductModelId BIGINT NULL;

IF COL_LENGTH('dbo.Product', 'SKU') IS NULL
    ALTER TABLE dbo.Product ADD SKU NVARCHAR(100) NULL;

IF COL_LENGTH('dbo.Product', 'Barcode') IS NULL
    ALTER TABLE dbo.Product ADD Barcode NVARCHAR(100) NULL;

IF COL_LENGTH('dbo.Product', 'RAM') IS NULL
    ALTER TABLE dbo.Product ADD RAM NVARCHAR(50) NULL;

IF COL_LENGTH('dbo.Product', 'Storage') IS NULL
    ALTER TABLE dbo.Product ADD Storage NVARCHAR(50) NULL;

IF COL_LENGTH('dbo.Product', 'Color') IS NULL
    ALTER TABLE dbo.Product ADD Color NVARCHAR(50) NULL;

IF COL_LENGTH('dbo.Product', 'MRP') IS NULL
    ALTER TABLE dbo.Product ADD MRP DECIMAL(18,2) NULL;

IF COL_LENGTH('dbo.Product', 'SellingPrice') IS NULL
    ALTER TABLE dbo.Product ADD SellingPrice DECIMAL(18,2) NULL;

IF COL_LENGTH('dbo.Product', 'WarrantyMonths') IS NULL
    ALTER TABLE dbo.Product ADD WarrantyMonths INT NOT NULL CONSTRAINT DF_Product_WarrantyMonths DEFAULT 0;

IF COL_LENGTH('dbo.Product', 'TrackIMEI') IS NULL
    ALTER TABLE dbo.Product ADD TrackIMEI BIT NOT NULL CONSTRAINT DF_Product_TrackIMEI DEFAULT 0;

IF COL_LENGTH('dbo.Product', 'ImagePath') IS NULL
    ALTER TABLE dbo.Product ADD ImagePath NVARCHAR(500) NULL;
GO

-- Unique Indexes for SKU & Barcode
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_Product_SKU' AND object_id = OBJECT_ID('dbo.Product'))
    CREATE UNIQUE NONCLUSTERED INDEX UQ_Product_SKU ON dbo.Product(SKU) 
    WHERE SKU IS NOT NULL AND SKU != '' AND IsDelete = 0;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_Product_Barcode' AND object_id = OBJECT_ID('dbo.Product'))
    CREATE UNIQUE NONCLUSTERED INDEX UQ_Product_Barcode ON dbo.Product(Barcode) 
    WHERE Barcode IS NOT NULL AND Barcode != '' AND IsDelete = 0;
GO

-- ============================================================================
-- MODULE 4: PAYMENT METHODS (SHARED ACROSS SALES & PURCHASES)
-- ============================================================================

IF OBJECT_ID('dbo.PaymentMethod', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.PaymentMethod (
        PaymentMethodId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PaymentMethod PRIMARY KEY CLUSTERED,
        MethodName NVARCHAR(100) NOT NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_PaymentMethod_IsActive DEFAULT 1
    );
END
GO

-- ============================================================================
-- MODULE 5: SUPPLIER & PURCHASE MANAGEMENT
-- ============================================================================

-- 5.1 Supplier
IF OBJECT_ID('dbo.Supplier', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Supplier (
        SupplierId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Supplier PRIMARY KEY CLUSTERED,
        SupplierName NVARCHAR(150) NOT NULL,
        ContactPerson NVARCHAR(100) NULL,
        Phone NVARCHAR(30) NULL,
        Email NVARCHAR(100) NULL,
        GSTNumber NVARCHAR(50) NULL,
        Address NVARCHAR(500) NULL,
        OpeningBalance DECIMAL(18,2) NOT NULL CONSTRAINT DF_Supplier_OpeningBalance DEFAULT 0.00,
        CurrentBalance DECIMAL(18,2) NOT NULL CONSTRAINT DF_Supplier_CurrentBalance DEFAULT 0.00,
        IsActive BIT NOT NULL CONSTRAINT DF_Supplier_IsActive DEFAULT 1,
        IsDelete BIT NOT NULL CONSTRAINT DF_Supplier_IsDelete DEFAULT 0,
        CreatedDate DATETIME2 NOT NULL CONSTRAINT DF_Supplier_CreatedDate DEFAULT SYSUTCDATETIME()
    );
END
GO

-- 5.2 Purchase Invoice
IF OBJECT_ID('dbo.Purchase', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Purchase (
        PurchaseId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Purchase PRIMARY KEY CLUSTERED,
        SupplierId BIGINT NOT NULL CONSTRAINT FK_Purchase_Supplier FOREIGN KEY REFERENCES dbo.Supplier(SupplierId),
        BranchId BIGINT NOT NULL CONSTRAINT FK_Purchase_Branch FOREIGN KEY REFERENCES dbo.Branch(BranchId),
        InvoiceNumber NVARCHAR(100) NOT NULL,
        PurchaseDate DATETIME2 NOT NULL,
        SubTotal DECIMAL(18,2) NOT NULL,
        Discount DECIMAL(18,2) NOT NULL CONSTRAINT DF_Purchase_Discount DEFAULT 0.00,
        TaxAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_Purchase_TaxAmount DEFAULT 0.00,
        GrandTotal DECIMAL(18,2) NOT NULL,
        PaidAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_Purchase_PaidAmount DEFAULT 0.00,
        DueAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_Purchase_DueAmount DEFAULT 0.00,
        Status NVARCHAR(50) NOT NULL CONSTRAINT DF_Purchase_Status DEFAULT 'Received', -- Received, Partial, Pending
        Notes NVARCHAR(500) NULL,
        CreatedBy INT NULL,
        CreatedDate DATETIME2 NOT NULL CONSTRAINT DF_Purchase_CreatedDate DEFAULT SYSUTCDATETIME()
    );
END
GO

-- 5.3 Purchase Items
IF OBJECT_ID('dbo.PurchaseItem', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.PurchaseItem (
        PurchaseItemId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PurchaseItem PRIMARY KEY CLUSTERED,
        PurchaseId BIGINT NOT NULL CONSTRAINT FK_PurchaseItem_Purchase FOREIGN KEY REFERENCES dbo.Purchase(PurchaseId) ON DELETE CASCADE,
        ProductId INT NOT NULL,
        Quantity INT NOT NULL,
        UnitCost DECIMAL(18,2) NOT NULL,
        Discount DECIMAL(18,2) NOT NULL CONSTRAINT DF_PurchaseItem_Discount DEFAULT 0.00,
        TaxAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_PurchaseItem_TaxAmount DEFAULT 0.00,
        TotalAmount DECIMAL(18,2) NOT NULL
    );
END
GO

-- 5.4 Purchase Payments (Supports multiple split payments to supplier)
IF OBJECT_ID('dbo.PurchasePayment', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.PurchasePayment (
        PurchasePaymentId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PurchasePayment PRIMARY KEY CLUSTERED,
        PurchaseId BIGINT NOT NULL CONSTRAINT FK_PurchasePayment_Purchase FOREIGN KEY REFERENCES dbo.Purchase(PurchaseId),
        PaymentMethodId INT NOT NULL CONSTRAINT FK_PurchasePayment_Method FOREIGN KEY REFERENCES dbo.PaymentMethod(PaymentMethodId),
        Amount DECIMAL(18,2) NOT NULL,
        TransactionReference NVARCHAR(100) NULL,
        PaymentDate DATETIME2 NOT NULL CONSTRAINT DF_PurchasePayment_PaymentDate DEFAULT SYSUTCDATETIME(),
        Notes NVARCHAR(500) NULL,
        CreatedBy INT NULL
    );
END
GO

-- ============================================================================
-- MODULE 6: IMEI TRACKING & INVENTORY MANAGEMENT
-- ============================================================================

-- 6.1 Individual Device Unit (Serial & Dual-IMEI Tracking)
-- CurrentStatus: 1=In Stock, 2=Reserved, 3=Sold, 4=Returned, 5=Under Repair, 6=Damaged
IF OBJECT_ID('dbo.DeviceUnit', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.DeviceUnit (
        DeviceUnitId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_DeviceUnit PRIMARY KEY CLUSTERED,
        ProductId INT NOT NULL,
        PurchaseItemId BIGINT NULL CONSTRAINT FK_DeviceUnit_PurchaseItem FOREIGN KEY REFERENCES dbo.PurchaseItem(PurchaseItemId),
        IMEI1 NVARCHAR(50) NULL,
        IMEI2 NVARCHAR(50) NULL,
        SerialNumber NVARCHAR(100) NULL,
        WarehouseId BIGINT NOT NULL CONSTRAINT FK_DeviceUnit_Warehouse FOREIGN KEY REFERENCES dbo.Warehouse(WarehouseId),
        CurrentStatus INT NOT NULL CONSTRAINT DF_DeviceUnit_CurrentStatus DEFAULT 1,
        WarrantyStartDate DATETIME2 NULL,
        WarrantyEndDate DATETIME2 NULL,
        CreatedDate DATETIME2 NOT NULL CONSTRAINT DF_DeviceUnit_CreatedDate DEFAULT SYSUTCDATETIME()
    );
END
GO

-- Unique Filtered Indexes on IMEI1 and IMEI2
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_DeviceUnit_IMEI1' AND object_id = OBJECT_ID('dbo.DeviceUnit'))
    CREATE UNIQUE NONCLUSTERED INDEX UQ_DeviceUnit_IMEI1 ON dbo.DeviceUnit(IMEI1) 
    WHERE IMEI1 IS NOT NULL AND IMEI1 != '';

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_DeviceUnit_IMEI2' AND object_id = OBJECT_ID('dbo.DeviceUnit'))
    CREATE UNIQUE NONCLUSTERED INDEX UQ_DeviceUnit_IMEI2 ON dbo.DeviceUnit(IMEI2) 
    WHERE IMEI2 IS NOT NULL AND IMEI2 != '';
GO

-- 6.2 Stock Balance (Fast transactional summary per product & warehouse)
IF OBJECT_ID('dbo.StockBalance', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.StockBalance (
        StockBalanceId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_StockBalance PRIMARY KEY CLUSTERED,
        ProductId INT NOT NULL,
        WarehouseId BIGINT NOT NULL CONSTRAINT FK_StockBalance_Warehouse FOREIGN KEY REFERENCES dbo.Warehouse(WarehouseId),
        QuantityOnHand INT NOT NULL CONSTRAINT DF_StockBalance_OnHand DEFAULT 0,
        QuantityReserved INT NOT NULL CONSTRAINT DF_StockBalance_Reserved DEFAULT 0,
        ReorderLevel INT NOT NULL CONSTRAINT DF_StockBalance_Reorder DEFAULT 5,
        LastUpdated DATETIME2 NOT NULL CONSTRAINT DF_StockBalance_LastUpdated DEFAULT SYSUTCDATETIME(),
        CONSTRAINT UQ_StockBalance_Product_Warehouse UNIQUE (ProductId, WarehouseId)
    );
END
GO

-- 6.3 Stock Movement (Audit trail of every stock change)
IF OBJECT_ID('dbo.StockMovement', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.StockMovement (
        StockMovementId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_StockMovement PRIMARY KEY CLUSTERED,
        WarehouseId BIGINT NOT NULL CONSTRAINT FK_StockMovement_Warehouse FOREIGN KEY REFERENCES dbo.Warehouse(WarehouseId),
        MovementType NVARCHAR(50) NOT NULL, -- 'PurchaseIn', 'SalesOut', 'ReturnIn', 'TransferOut', 'TransferIn', 'Adjustment'
        ReferenceType NVARCHAR(50) NOT NULL, -- 'Purchase', 'SalesInvoice', 'SalesReturn', 'StockTransfer'
        ReferenceId BIGINT NOT NULL,
        MovementDate DATETIME2 NOT NULL CONSTRAINT DF_StockMovement_Date DEFAULT SYSUTCDATETIME(),
        Notes NVARCHAR(500) NULL,
        CreatedBy INT NULL
    );
END
GO

-- 6.4 Stock Movement Items
IF OBJECT_ID('dbo.StockMovementItem', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.StockMovementItem (
        StockMovementItemId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_StockMovementItem PRIMARY KEY CLUSTERED,
        StockMovementId BIGINT NOT NULL CONSTRAINT FK_StockMovementItem_Movement FOREIGN KEY REFERENCES dbo.StockMovement(StockMovementId) ON DELETE CASCADE,
        ProductId INT NOT NULL,
        QuantityIn INT NOT NULL CONSTRAINT DF_StockMovementItem_In DEFAULT 0,
        QuantityOut INT NOT NULL CONSTRAINT DF_StockMovementItem_Out DEFAULT 0,
        UnitCost DECIMAL(18,2) NOT NULL CONSTRAINT DF_StockMovementItem_UnitCost DEFAULT 0.00
    );
END
GO

-- 6.5 Stock Movement Serials (Specific DeviceUnits involved in movement)
IF OBJECT_ID('dbo.StockMovementSerial', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.StockMovementSerial (
        MovementSerialId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_StockMovementSerial PRIMARY KEY CLUSTERED,
        StockMovementItemId BIGINT NOT NULL CONSTRAINT FK_StockMovementSerial_Item FOREIGN KEY REFERENCES dbo.StockMovementItem(StockMovementItemId) ON DELETE CASCADE,
        DeviceUnitId BIGINT NOT NULL CONSTRAINT FK_StockMovementSerial_Unit FOREIGN KEY REFERENCES dbo.DeviceUnit(DeviceUnitId)
    );
END
GO

-- ============================================================================
-- MODULE 7: CUSTOMER & SALES MANAGEMENT
-- ============================================================================

-- 7.1 Customer
IF OBJECT_ID('dbo.Customer', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Customer (
        CustomerId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Customer PRIMARY KEY CLUSTERED,
        CustomerName NVARCHAR(150) NOT NULL,
        Phone NVARCHAR(30) NULL,
        Email NVARCHAR(100) NULL,
        GSTNumber NVARCHAR(50) NULL,
        Address NVARCHAR(500) NULL,
        OpeningBalance DECIMAL(18,2) NOT NULL CONSTRAINT DF_Customer_OpeningBalance DEFAULT 0.00,
        CurrentBalance DECIMAL(18,2) NOT NULL CONSTRAINT DF_Customer_CurrentBalance DEFAULT 0.00,
        IsActive BIT NOT NULL CONSTRAINT DF_Customer_IsActive DEFAULT 1,
        IsDelete BIT NOT NULL CONSTRAINT DF_Customer_IsDelete DEFAULT 0,
        CreatedDate DATETIME2 NOT NULL CONSTRAINT DF_Customer_CreatedDate DEFAULT SYSUTCDATETIME()
    );
END
GO

-- 7.2 Sales Invoice (Billing)
IF OBJECT_ID('dbo.SalesInvoice', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.SalesInvoice (
        SalesInvoiceId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SalesInvoice PRIMARY KEY CLUSTERED,
        BranchId BIGINT NOT NULL CONSTRAINT FK_SalesInvoice_Branch FOREIGN KEY REFERENCES dbo.Branch(BranchId),
        CustomerId BIGINT NULL CONSTRAINT FK_SalesInvoice_Customer FOREIGN KEY REFERENCES dbo.Customer(CustomerId),
        InvoiceNumber NVARCHAR(100) NOT NULL CONSTRAINT UQ_SalesInvoice_Number UNIQUE,
        InvoiceDate DATETIME2 NOT NULL CONSTRAINT DF_SalesInvoice_Date DEFAULT SYSUTCDATETIME(),
        SubTotal DECIMAL(18,2) NOT NULL,
        Discount DECIMAL(18,2) NOT NULL CONSTRAINT DF_SalesInvoice_Discount DEFAULT 0.00,
        TaxAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_SalesInvoice_TaxAmount DEFAULT 0.00,
        GrandTotal DECIMAL(18,2) NOT NULL,
        PaidAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_SalesInvoice_PaidAmount DEFAULT 0.00,
        DueAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_SalesInvoice_DueAmount DEFAULT 0.00,
        Status NVARCHAR(50) NOT NULL CONSTRAINT DF_SalesInvoice_Status DEFAULT 'Paid', -- Paid, Partial, Unpaid, Cancelled
        Notes NVARCHAR(500) NULL,
        CreatedBy INT NULL,
        CreatedDate DATETIME2 NOT NULL CONSTRAINT DF_SalesInvoice_CreatedDate DEFAULT SYSUTCDATETIME()
    );
END
GO

-- 7.3 Sales Invoice Items
IF OBJECT_ID('dbo.SalesInvoiceItem', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.SalesInvoiceItem (
        SalesInvoiceItemId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SalesInvoiceItem PRIMARY KEY CLUSTERED,
        SalesInvoiceId BIGINT NOT NULL CONSTRAINT FK_SalesInvoiceItem_Invoice FOREIGN KEY REFERENCES dbo.SalesInvoice(SalesInvoiceId) ON DELETE CASCADE,
        ProductId INT NOT NULL,
        Quantity INT NOT NULL,
        UnitPrice DECIMAL(18,2) NOT NULL,
        Discount DECIMAL(18,2) NOT NULL CONSTRAINT DF_SalesInvoiceItem_Discount DEFAULT 0.00,
        TaxAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_SalesInvoiceItem_TaxAmount DEFAULT 0.00,
        TotalAmount DECIMAL(18,2) NOT NULL
    );
END
GO

-- 7.4 Sales Item Serials (Links exact sold phone IMEI to invoice line item)
IF OBJECT_ID('dbo.SalesItemSerial', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.SalesItemSerial (
        SalesItemSerialId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SalesItemSerial PRIMARY KEY CLUSTERED,
        SalesInvoiceItemId BIGINT NOT NULL CONSTRAINT FK_SalesItemSerial_Item FOREIGN KEY REFERENCES dbo.SalesInvoiceItem(SalesInvoiceItemId) ON DELETE CASCADE,
        DeviceUnitId BIGINT NOT NULL CONSTRAINT FK_SalesItemSerial_Unit FOREIGN KEY REFERENCES dbo.DeviceUnit(DeviceUnitId),
        IsReturned BIT NOT NULL CONSTRAINT DF_SalesItemSerial_IsReturned DEFAULT 0
    );
END
GO

-- 7.5 Sales Payments (Multiple split payments per invoice: Cash, UPI, Card, etc.)
IF OBJECT_ID('dbo.SalesPayment', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.SalesPayment (
        SalesPaymentId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SalesPayment PRIMARY KEY CLUSTERED,
        SalesInvoiceId BIGINT NOT NULL CONSTRAINT FK_SalesPayment_Invoice FOREIGN KEY REFERENCES dbo.SalesInvoice(SalesInvoiceId),
        PaymentMethodId INT NOT NULL CONSTRAINT FK_SalesPayment_Method FOREIGN KEY REFERENCES dbo.PaymentMethod(PaymentMethodId),
        Amount DECIMAL(18,2) NOT NULL,
        TransactionReference NVARCHAR(100) NULL,
        PaymentDate DATETIME2 NOT NULL CONSTRAINT DF_SalesPayment_Date DEFAULT SYSUTCDATETIME(),
        CreatedBy INT NULL
    );
END
GO

-- ============================================================================
-- MODULE 8: RETURNS & EXCHANGES MANAGEMENT
-- ============================================================================

-- 8.1 Sales Return
IF OBJECT_ID('dbo.SalesReturn', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.SalesReturn (
        SalesReturnId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SalesReturn PRIMARY KEY CLUSTERED,
        SalesInvoiceId BIGINT NOT NULL CONSTRAINT FK_SalesReturn_Invoice FOREIGN KEY REFERENCES dbo.SalesInvoice(SalesInvoiceId),
        CustomerId BIGINT NULL CONSTRAINT FK_SalesReturn_Customer FOREIGN KEY REFERENCES dbo.Customer(CustomerId),
        ReturnDate DATETIME2 NOT NULL CONSTRAINT DF_SalesReturn_Date DEFAULT SYSUTCDATETIME(),
        Reason NVARCHAR(500) NULL,
        RefundAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_SalesReturn_Refund DEFAULT 0.00,
        Status NVARCHAR(50) NOT NULL CONSTRAINT DF_SalesReturn_Status DEFAULT 'Approved',
        CreatedBy INT NULL,
        CreatedDate DATETIME2 NOT NULL CONSTRAINT DF_SalesReturn_CreatedDate DEFAULT SYSUTCDATETIME()
    );
END
GO

-- 8.2 Sales Return Items
IF OBJECT_ID('dbo.SalesReturnItem', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.SalesReturnItem (
        SalesReturnItemId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SalesReturnItem PRIMARY KEY CLUSTERED,
        SalesReturnId BIGINT NOT NULL CONSTRAINT FK_SalesReturnItem_Return FOREIGN KEY REFERENCES dbo.SalesReturn(SalesReturnId) ON DELETE CASCADE,
        SalesInvoiceItemId BIGINT NULL CONSTRAINT FK_SalesReturnItem_InvoiceItem FOREIGN KEY REFERENCES dbo.SalesInvoiceItem(SalesInvoiceItemId),
        ProductId INT NOT NULL,
        Quantity INT NOT NULL,
        ReturnCondition NVARCHAR(50) NOT NULL CONSTRAINT DF_SalesReturnItem_Condition DEFAULT 'Good', -- Good, Defective, Damaged
        RefundAmount DECIMAL(18,2) NOT NULL
    );
END
GO

-- 8.3 Sales Return Serials (Tracks returned phone IMEI)
IF OBJECT_ID('dbo.SalesReturnSerial', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.SalesReturnSerial (
        ReturnSerialId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SalesReturnSerial PRIMARY KEY CLUSTERED,
        SalesReturnItemId BIGINT NOT NULL CONSTRAINT FK_SalesReturnSerial_Item FOREIGN KEY REFERENCES dbo.SalesReturnItem(SalesReturnItemId) ON DELETE CASCADE,
        DeviceUnitId BIGINT NOT NULL CONSTRAINT FK_SalesReturnSerial_Unit FOREIGN KEY REFERENCES dbo.DeviceUnit(DeviceUnitId)
    );
END
GO

-- 8.4 Sales Return Payment (Refunds disbursed)
IF OBJECT_ID('dbo.SalesReturnPayment', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.SalesReturnPayment (
        ReturnPaymentId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SalesReturnPayment PRIMARY KEY CLUSTERED,
        SalesReturnId BIGINT NOT NULL CONSTRAINT FK_SalesReturnPayment_Return FOREIGN KEY REFERENCES dbo.SalesReturn(SalesReturnId),
        PaymentMethodId INT NOT NULL CONSTRAINT FK_SalesReturnPayment_Method FOREIGN KEY REFERENCES dbo.PaymentMethod(PaymentMethodId),
        Amount DECIMAL(18,2) NOT NULL,
        PaymentDate DATETIME2 NOT NULL CONSTRAINT DF_SalesReturnPayment_Date DEFAULT SYSUTCDATETIME(),
        CreatedBy INT NULL
    );
END
GO

-- 8.5 Sales Exchange (Exchange returned device for an upgrade/different phone)
IF OBJECT_ID('dbo.SalesExchange', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.SalesExchange (
        ExchangeId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SalesExchange PRIMARY KEY CLUSTERED,
        SalesReturnId BIGINT NOT NULL CONSTRAINT FK_SalesExchange_Return FOREIGN KEY REFERENCES dbo.SalesReturn(SalesReturnId),
        NewSalesInvoiceId BIGINT NOT NULL CONSTRAINT FK_SalesExchange_Invoice FOREIGN KEY REFERENCES dbo.SalesInvoice(SalesInvoiceId),
        ExchangeDifference DECIMAL(18,2) NOT NULL,
        ExchangeDate DATETIME2 NOT NULL CONSTRAINT DF_SalesExchange_Date DEFAULT SYSUTCDATETIME()
    );
END
GO

-- ============================================================================
-- MODULE 9: EXPENSES, WARRANTY & REPAIRS
-- ============================================================================

-- 9.1 Expense Category
IF OBJECT_ID('dbo.ExpenseCategory', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ExpenseCategory (
        ExpenseCategoryId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ExpenseCategory PRIMARY KEY CLUSTERED,
        CategoryName NVARCHAR(100) NOT NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_ExpenseCategory_IsActive DEFAULT 1
    );
END
GO

-- 9.2 Expense
IF OBJECT_ID('dbo.Expense', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Expense (
        ExpenseId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Expense PRIMARY KEY CLUSTERED,
        ExpenseCategoryId INT NOT NULL CONSTRAINT FK_Expense_Category FOREIGN KEY REFERENCES dbo.ExpenseCategory(ExpenseCategoryId),
        BranchId BIGINT NOT NULL CONSTRAINT FK_Expense_Branch FOREIGN KEY REFERENCES dbo.Branch(BranchId),
        ExpenseDate DATETIME2 NOT NULL CONSTRAINT DF_Expense_Date DEFAULT SYSUTCDATETIME(),
        Amount DECIMAL(18,2) NOT NULL,
        Description NVARCHAR(500) NULL,
        PaymentMethodId INT NOT NULL CONSTRAINT FK_Expense_Method FOREIGN KEY REFERENCES dbo.PaymentMethod(PaymentMethodId),
        CreatedBy INT NULL,
        CreatedDate DATETIME2 NOT NULL CONSTRAINT DF_Expense_CreatedDate DEFAULT SYSUTCDATETIME()
    );
END
ELSE
BEGIN
    IF COL_LENGTH('dbo.Expense', 'CreatedDate') IS NULL
        ALTER TABLE dbo.Expense ADD CreatedDate DATETIME2 NOT NULL CONSTRAINT DF_Expense_CreatedDate DEFAULT SYSUTCDATETIME();
END
GO

-- 9.3 Warranty Claims
IF OBJECT_ID('dbo.WarrantyClaim', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.WarrantyClaim (
        WarrantyClaimId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_WarrantyClaim PRIMARY KEY CLUSTERED,
        DeviceUnitId BIGINT NOT NULL CONSTRAINT FK_WarrantyClaim_Unit FOREIGN KEY REFERENCES dbo.DeviceUnit(DeviceUnitId),
        CustomerId BIGINT NULL CONSTRAINT FK_WarrantyClaim_Customer FOREIGN KEY REFERENCES dbo.Customer(CustomerId),
        SalesInvoiceItemId BIGINT NULL CONSTRAINT FK_WarrantyClaim_InvoiceItem FOREIGN KEY REFERENCES dbo.SalesInvoiceItem(SalesInvoiceItemId),
        ClaimDate DATETIME2 NOT NULL CONSTRAINT DF_WarrantyClaim_Date DEFAULT SYSUTCDATETIME(),
        IssueDescription NVARCHAR(MAX) NOT NULL,
        Status NVARCHAR(50) NOT NULL CONSTRAINT DF_WarrantyClaim_Status DEFAULT 'Pending', -- Pending, InProcess, Approved, Rejected, Resolved
        CreatedBy INT NULL
    );
END
GO

-- 9.4 Repair Jobs (Paid & Warranty Mobile Repairs)
IF OBJECT_ID('dbo.RepairJob', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.RepairJob (
        RepairJobId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_RepairJob PRIMARY KEY CLUSTERED,
        WarrantyClaimId BIGINT NULL CONSTRAINT FK_RepairJob_Warranty FOREIGN KEY REFERENCES dbo.WarrantyClaim(WarrantyClaimId),
        CustomerId BIGINT NULL CONSTRAINT FK_RepairJob_Customer FOREIGN KEY REFERENCES dbo.Customer(CustomerId),
        DeviceUnitId BIGINT NULL CONSTRAINT FK_RepairJob_Unit FOREIGN KEY REFERENCES dbo.DeviceUnit(DeviceUnitId),
        DeviceModelName NVARCHAR(150) NULL,
        IMEIOrSerial NVARCHAR(100) NULL,
        ReceivedDate DATETIME2 NOT NULL CONSTRAINT DF_RepairJob_ReceivedDate DEFAULT SYSUTCDATETIME(),
        ProblemDescription NVARCHAR(MAX) NOT NULL,
        EstimatedCost DECIMAL(18,2) NOT NULL CONSTRAINT DF_RepairJob_Estimated DEFAULT 0.00,
        RepairCost DECIMAL(18,2) NOT NULL CONSTRAINT DF_RepairJob_RepairCost DEFAULT 0.00,
        Status NVARCHAR(50) NOT NULL CONSTRAINT DF_RepairJob_Status DEFAULT 'Received', -- Received, Diagnosing, WaitingForParts, InRepair, Completed, Delivered, Cancelled
        CompletedDate DATETIME2 NULL,
        DeliveredDate DATETIME2 NULL,
        TechnicianNotes NVARCHAR(MAX) NULL,
        CreatedBy INT NULL
    );
END
GO

-- 9.5 Repair Job Parts (Spare parts consumed during repair)
IF OBJECT_ID('dbo.RepairJobPart', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.RepairJobPart (
        RepairJobPartId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_RepairJobPart PRIMARY KEY CLUSTERED,
        RepairJobId BIGINT NOT NULL CONSTRAINT FK_RepairJobPart_Job FOREIGN KEY REFERENCES dbo.RepairJob(RepairJobId) ON DELETE CASCADE,
        ProductId INT NOT NULL, -- Screen, Battery, Charging Port, etc.
        Quantity INT NOT NULL CONSTRAINT DF_RepairJobPart_Qty DEFAULT 1,
        UnitCost DECIMAL(18,2) NOT NULL,
        TotalCost DECIMAL(18,2) NOT NULL
    );
END
GO

-- 9.6 Repair Status History (Audit trail of job states)
IF OBJECT_ID('dbo.RepairStatusHistory', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.RepairStatusHistory (
        HistoryId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_RepairStatusHistory PRIMARY KEY CLUSTERED,
        RepairJobId BIGINT NOT NULL CONSTRAINT FK_RepairStatusHistory_Job FOREIGN KEY REFERENCES dbo.RepairJob(RepairJobId) ON DELETE CASCADE,
        PreviousStatus NVARCHAR(50) NULL,
        NewStatus NVARCHAR(50) NOT NULL,
        Remarks NVARCHAR(500) NULL,
        ChangedAt DATETIME2 NOT NULL CONSTRAINT DF_RepairHistory_ChangedAt DEFAULT SYSUTCDATETIME(),
        ChangedBy INT NULL
    );
END
GO

-- ============================================================================
-- INITIAL SEED DATA (CORE SETUP)
-- ============================================================================

-- Seed Default Company
IF NOT EXISTS (SELECT 1 FROM dbo.Company)
BEGIN
    INSERT INTO dbo.Company (CompanyName, GSTNumber, Address, Phone, Email, LogoPath, IsActive)
    VALUES ('SmartCell Mobile Store', '27AABCU9603R1ZM', 'Shop No. 12, Main Market Road', '+91 98765 43210', 'support@smartcell.com', '/images/logo.png', 1);
END
GO

-- Seed Default Branch
IF NOT EXISTS (SELECT 1 FROM dbo.Branch)
BEGIN
    DECLARE @DefaultCompanyId BIGINT = (SELECT TOP 1 CompanyId FROM dbo.Company);
    INSERT INTO dbo.Branch (CompanyId, BranchName, BranchCode, Address, Phone, Email, IsActive)
    VALUES (@DefaultCompanyId, 'Main Showroom Branch', 'HQ-01', 'Shop No. 12, Main Market Road', '+91 98765 43210', 'hq@smartcell.com', 1);
END
GO

-- Seed Default Warehouse
IF NOT EXISTS (SELECT 1 FROM dbo.Warehouse)
BEGIN
    DECLARE @DefaultBranchId BIGINT = (SELECT TOP 1 BranchId FROM dbo.Branch);
    INSERT INTO dbo.Warehouse (BranchId, WarehouseName, Location, IsActive)
    VALUES (@DefaultBranchId, 'Main Showroom Inventory', 'Display & Main Stock Room', 1);
END
GO

-- Seed Standard Payment Methods
IF NOT EXISTS (SELECT 1 FROM dbo.PaymentMethod)
BEGIN
    INSERT INTO dbo.PaymentMethod (MethodName, IsActive)
    VALUES ('Cash', 1),
           ('UPI', 1),
           ('Google Pay', 1),
           ('PhonePe', 1),
           ('Paytm', 1),
           ('Credit/Debit Card', 1),
           ('Bank Transfer', 1),
           ('Customer Credit', 1);
END
GO

-- Seed Standard Roles
IF NOT EXISTS (SELECT 1 FROM dbo.Role)
BEGIN
    INSERT INTO dbo.Role (RoleName, Description, IsActive)
    VALUES ('Super Admin', 'Full system access and master configuration', 1),
           ('Shop Owner', 'Owner level access to all branches, financials and reports', 1),
           ('Store Manager', 'Daily operational manager for sales, purchases and repairs', 1),
           ('Sales Executive', 'POS billing, sales invoices and IMEI validation', 1),
           ('Technician', 'Service, warranty and phone repair jobs', 1),
           ('Accountant', 'Purchases, expenses and financial reports', 1);
END
GO

-- Seed Standard Expense Categories
IF NOT EXISTS (SELECT 1 FROM dbo.ExpenseCategory)
BEGIN
    INSERT INTO dbo.ExpenseCategory (CategoryName, IsActive)
    VALUES ('Shop Rent', 1),
           ('Electricity Bill', 1),
           ('Staff Salary', 1),
           ('Internet & Telephony', 1),
           ('Transportation & Courier', 1),
           ('Packaging & Bags', 1),
           ('Office Supplies & Tea', 1),
           ('Equipment Maintenance', 1);
END
GO
