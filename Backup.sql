/* ============================================================================
   Complete Self-Contained SQL Server Database Backup Script
   Database        : [Product]
   Generated On    : 2026-11-26 18:01:42 +05:30
   Compatibility   : 160 (auto-adapts if target SQL Server is older)
   Collation       : SQL_Latin1_General_CP1_CI_AS
   Tables          : 39
   Stored Procs    : 16
   Foreign Keys    : 46
   Indexes         : 48
   ============================================================================
   HOW TO RESTORE ON ANOTHER PC:
   1. Open SQL Server Management Studio (SSMS) or Azure Data Studio.
   2. Connect to your target SQL Server instance.
   3. Open this Backup.sql file and execute the entire script (F5),
      OR run via command line:
      sqlcmd -S . -E -i "Backup.sql"
   ============================================================================ */

SET NOCOUNT ON;
GO

USE [master];
GO

-- Guard: Prevent accidental overwrite if [Product] already exists
IF DB_ID(N'Product') IS NOT NULL
BEGIN
    RAISERROR(N'Database [Product] already exists on this server. Drop or rename the existing database first before running this backup script.', 16, 1);
    SET NOEXEC ON;
END
GO

-- ============================================================================
-- 1. CREATE DATABASE & CONFIGURE DATABASE SETTINGS
-- ============================================================================
DECLARE @DataPath NVARCHAR(512) = CAST(SERVERPROPERTY('InstanceDefaultDataPath') AS NVARCHAR(512));
DECLARE @LogPath  NVARCHAR(512) = CAST(SERVERPROPERTY('InstanceDefaultLogPath')  AS NVARCHAR(512));

IF @DataPath IS NULL OR LTRIM(RTRIM(@DataPath)) = N''
BEGIN
    SELECT TOP 1 @DataPath = LEFT(physical_name, LEN(physical_name) - CHARINDEX(N'\', REVERSE(physical_name)) + 1)
    FROM sys.master_files WHERE database_id = 1 AND type = 0;
END
IF @LogPath IS NULL OR LTRIM(RTRIM(@LogPath)) = N''
BEGIN
    SELECT TOP 1 @LogPath = LEFT(physical_name, LEN(physical_name) - CHARINDEX(N'\', REVERSE(physical_name)) + 1)
    FROM sys.master_files WHERE database_id = 1 AND type = 1;
END

IF RIGHT(@DataPath, 1) NOT IN (N'\', N'/') SET @DataPath += N'\';
IF RIGHT(@LogPath, 1)  NOT IN (N'\', N'/') SET @LogPath  += N'\';

DECLARE @MajorVer INT = CAST(SERVERPROPERTY('ProductMajorVersion') AS INT);
DECLARE @CreateDbSql NVARCHAR(MAX) = N'
CREATE DATABASE [Product]
 CONTAINMENT = NONE
 ON PRIMARY
( NAME = N''Product'', FILENAME = N''' + REPLACE(@DataPath, '''', '''''') + N'Product.mdf'', SIZE = 8192KB, MAXSIZE = UNLIMITED, FILEGROWTH = 65536KB )
 LOG ON
( NAME = N''Product_log'', FILENAME = N''' + REPLACE(@LogPath, '''', '''''') + N'Product_log.ldf'', SIZE = 8192KB, MAXSIZE = 2048GB, FILEGROWTH = 65536KB )
 COLLATE SQL_Latin1_General_CP1_CI_AS';

IF @MajorVer >= 16
    SET @CreateDbSql += N' WITH CATALOG_COLLATION = DATABASE_DEFAULT, LEDGER = OFF;';
ELSE IF @MajorVer >= 15
    SET @CreateDbSql += N' WITH CATALOG_COLLATION = DATABASE_DEFAULT;';
ELSE
    SET @CreateDbSql += N';';

EXEC sp_executesql @CreateDbSql;
GO

IF DB_ID(N'Product') IS NOT NULL
BEGIN
    DECLARE @MajorVer2 INT = CAST(SERVERPROPERTY('ProductMajorVersion') AS INT);
    DECLARE @Compat INT = CASE WHEN @MajorVer2 * 10 < 160 THEN @MajorVer2 * 10 ELSE 160 END;
    DECLARE @CompatSql NVARCHAR(200) = N'ALTER DATABASE [Product] SET COMPATIBILITY_LEVEL = ' + CAST(@Compat AS NVARCHAR(10)) + N';';
    EXEC sp_executesql @CompatSql;
END
GO
IF (1 = FULLTEXTSERVICEPROPERTY('IsFullTextInstalled'))
BEGIN
    EXEC [Product].[dbo].[sp_fulltext_database] @action = 'enable';
END
GO

ALTER DATABASE [Product] SET ANSI_NULL_DEFAULT OFF;
ALTER DATABASE [Product] SET ANSI_NULLS OFF;
ALTER DATABASE [Product] SET ANSI_PADDING OFF;
ALTER DATABASE [Product] SET ANSI_WARNINGS OFF;
ALTER DATABASE [Product] SET ARITHABORT OFF;
ALTER DATABASE [Product] SET AUTO_CLOSE OFF;
ALTER DATABASE [Product] SET AUTO_SHRINK OFF;
ALTER DATABASE [Product] SET AUTO_CREATE_STATISTICS ON;
ALTER DATABASE [Product] SET AUTO_UPDATE_STATISTICS ON;
ALTER DATABASE [Product] SET AUTO_UPDATE_STATISTICS_ASYNC OFF;
ALTER DATABASE [Product] SET CURSOR_CLOSE_ON_COMMIT OFF;
ALTER DATABASE [Product] SET CURSOR_DEFAULT GLOBAL;
ALTER DATABASE [Product] SET CONCAT_NULL_YIELDS_NULL OFF;
ALTER DATABASE [Product] SET NUMERIC_ROUNDABORT OFF;
ALTER DATABASE [Product] SET QUOTED_IDENTIFIER OFF;
ALTER DATABASE [Product] SET RECURSIVE_TRIGGERS OFF;
ALTER DATABASE [Product] SET DISABLE_BROKER;
ALTER DATABASE [Product] SET DATE_CORRELATION_OPTIMIZATION OFF;
ALTER DATABASE [Product] SET TRUSTWORTHY OFF;
ALTER DATABASE [Product] SET ALLOW_SNAPSHOT_ISOLATION OFF;
ALTER DATABASE [Product] SET PARAMETERIZATION SIMPLE;
ALTER DATABASE [Product] SET READ_COMMITTED_SNAPSHOT OFF;
ALTER DATABASE [Product] SET HONOR_BROKER_PRIORITY OFF;
ALTER DATABASE [Product] SET RECOVERY FULL;
ALTER DATABASE [Product] SET MULTI_USER;
ALTER DATABASE [Product] SET PAGE_VERIFY CHECKSUM;
ALTER DATABASE [Product] SET DB_CHAINING OFF;
ALTER DATABASE [Product] SET TARGET_RECOVERY_TIME = 60 SECONDS;
ALTER DATABASE [Product] SET DELAYED_DURABILITY = DISABLED;
GO

IF CAST(SERVERPROPERTY('ProductMajorVersion') AS INT) >= 15
BEGIN
    EXEC(N'ALTER DATABASE [Product] SET ACCELERATED_DATABASE_RECOVERY = OFF;');
END
GO

IF CAST(SERVERPROPERTY('ProductMajorVersion') AS INT) >= 15
BEGIN
    EXEC(N'ALTER DATABASE [Product] SET QUERY_STORE = ON;');
    EXEC(N'ALTER DATABASE [Product] SET QUERY_STORE (OPERATION_MODE = READ_WRITE, CLEANUP_POLICY = (STALE_QUERY_THRESHOLD_DAYS = 30), DATA_FLUSH_INTERVAL_SECONDS = 900, INTERVAL_LENGTH_MINUTES = 60, MAX_STORAGE_SIZE_MB = 1000, QUERY_CAPTURE_MODE = AUTO, SIZE_BASED_CLEANUP_MODE = AUTO, MAX_PLANS_PER_QUERY = 200, WAIT_STATS_CAPTURE_MODE = ON);');
END
GO

ALTER DATABASE [Product] SET READ_WRITE;
GO

USE [Product];
GO

-- ============================================================================
-- 2. CREATE TABLES, PRIMARY KEYS, CONSTRAINTS, AND INDEXES
-- ============================================================================

-- ----------------------------------------------------------------------------
-- Table: [dbo].[Brand]
-- ----------------------------------------------------------------------------
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
SET ANSI_PADDING ON;
GO
CREATE TABLE [dbo].[Brand] (
    [BrandId] [int] IDENTITY(1, 1) NOT NULL,
    [BrandName] [varchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
    [IsActive] [bit] NOT NULL,
    [IsDelete] [bit] NOT NULL,
    [CreatedDate] [datetime] NOT NULL,
    [UserId] [int] NULL,
    [ImageUrl] [nvarchar](500) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
    [Image] [nvarchar](500) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
    CONSTRAINT [PK__Brand__DAD4F05E9AA36652] PRIMARY KEY CLUSTERED
    (
        [BrandId] ASC
    ) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY],
    CONSTRAINT [UQ__Brand__2206CE9B3088448C] UNIQUE NONCLUSTERED
    (
        [BrandName] ASC
    ) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY];
GO
ALTER TABLE [dbo].[Brand] ADD CONSTRAINT [DF__Brand__CreatedDa__52593CB8] DEFAULT (getdate()) FOR [CreatedDate];
ALTER TABLE [dbo].[Brand] ADD CONSTRAINT [DF__Brand__IsActive__5070F446] DEFAULT ((1)) FOR [IsActive];
ALTER TABLE [dbo].[Brand] ADD CONSTRAINT [DF__Brand__IsDelete__5165187F] DEFAULT ((0)) FOR [IsDelete];
GO

-- ----------------------------------------------------------------------------
-- Table: [dbo].[Category]
-- ----------------------------------------------------------------------------
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
SET ANSI_PADDING ON;
GO
CREATE TABLE [dbo].[Category] (
    [CategoryId] [int] IDENTITY(1, 1) NOT NULL,
    [CategoryName] [varchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
    [IsActive] [bit] NOT NULL,
    [IsDelete] [bit] NOT NULL,
    [CreatedDate] [datetime] NOT NULL,
    [UserId] [int] NULL,
    [ImageUrl] [nvarchar](500) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
    [Image] [nvarchar](500) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
    CONSTRAINT [PK__Category__19093A0B85FB567C] PRIMARY KEY CLUSTERED
    (
        [CategoryId] ASC
    ) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY],
    CONSTRAINT [UQ__Category__8517B2E01E9E7CF8] UNIQUE NONCLUSTERED
    (
        [CategoryName] ASC
    ) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY];
GO
ALTER TABLE [dbo].[Category] ADD CONSTRAINT [DF__Category__Create__4CA06362] DEFAULT (getdate()) FOR [CreatedDate];
ALTER TABLE [dbo].[Category] ADD CONSTRAINT [DF__Category__IsActi__4AB81AF0] DEFAULT ((1)) FOR [IsActive];
ALTER TABLE [dbo].[Category] ADD CONSTRAINT [DF__Category__IsDele__4BAC3F29] DEFAULT ((0)) FOR [IsDelete];
GO

-- ----------------------------------------------------------------------------
-- Table: [dbo].[Company]
-- ----------------------------------------------------------------------------
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
SET ANSI_PADDING ON;
GO
CREATE TABLE [dbo].[Company] (
    [CompanyId] [bigint] IDENTITY(1, 1) NOT NULL,
    [CompanyName] [nvarchar](150) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
    [GSTNumber] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
    [Address] [nvarchar](500) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
    [Phone] [nvarchar](30) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
    [Email] [nvarchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
    [LogoPath] [nvarchar](500) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
    [IsActive] [bit] NOT NULL,
    [CreatedDate] [datetime2](7) NOT NULL,
    CONSTRAINT [PK_Company] PRIMARY KEY CLUSTERED
    (
        [CompanyId] ASC
    ) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY];
GO
ALTER TABLE [dbo].[Company] ADD CONSTRAINT [DF_Company_CreatedDate] DEFAULT (sysutcdatetime()) FOR [CreatedDate];
ALTER TABLE [dbo].[Company] ADD CONSTRAINT [DF_Company_IsActive] DEFAULT ((1)) FOR [IsActive];
GO

-- ----------------------------------------------------------------------------
-- Table: [dbo].[Customer]
-- ----------------------------------------------------------------------------
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
SET ANSI_PADDING ON;
GO
CREATE TABLE [dbo].[Customer] (
    [CustomerId] [bigint] IDENTITY(1, 1) NOT NULL,
    [CustomerName] [nvarchar](150) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
    [Phone] [nvarchar](30) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
    [Email] [nvarchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
    [GSTNumber] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
    [Address] [nvarchar](500) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
    [OpeningBalance] [decimal](18, 2) NOT NULL,
    [CurrentBalance] [decimal](18, 2) NOT NULL,
    [IsActive] [bit] NOT NULL,
    [IsDelete] [bit] NOT NULL,
    [CreatedDate] [datetime2](7) NOT NULL,
    [UserId] [int] NULL,
    CONSTRAINT [PK_Customer] PRIMARY KEY CLUSTERED
    (
        [CustomerId] ASC
    ) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY];
GO
ALTER TABLE [dbo].[Customer] ADD CONSTRAINT [DF_Customer_CreatedDate] DEFAULT (sysutcdatetime()) FOR [CreatedDate];
ALTER TABLE [dbo].[Customer] ADD CONSTRAINT [DF_Customer_CurrentBalance] DEFAULT ((0.00)) FOR [CurrentBalance];
ALTER TABLE [dbo].[Customer] ADD CONSTRAINT [DF_Customer_IsActive] DEFAULT ((1)) FOR [IsActive];
ALTER TABLE [dbo].[Customer] ADD CONSTRAINT [DF_Customer_IsDelete] DEFAULT ((0)) FOR [IsDelete];
ALTER TABLE [dbo].[Customer] ADD CONSTRAINT [DF_Customer_OpeningBalance] DEFAULT ((0.00)) FOR [OpeningBalance];
GO

-- ----------------------------------------------------------------------------
-- Table: [dbo].[ExpenseCategory]
-- ----------------------------------------------------------------------------
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
CREATE TABLE [dbo].[ExpenseCategory] (
    [ExpenseCategoryId] [int] IDENTITY(1, 1) NOT NULL,
    [CategoryName] [nvarchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
    [IsActive] [bit] NOT NULL,
    CONSTRAINT [PK_ExpenseCategory] PRIMARY KEY CLUSTERED
    (
        [ExpenseCategoryId] ASC
    ) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY];
GO
ALTER TABLE [dbo].[ExpenseCategory] ADD CONSTRAINT [DF_ExpenseCategory_IsActive] DEFAULT ((1)) FOR [IsActive];
GO

-- ----------------------------------------------------------------------------
-- Table: [dbo].[PaymentMethod]
-- ----------------------------------------------------------------------------
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
CREATE TABLE [dbo].[PaymentMethod] (
    [PaymentMethodId] [int] IDENTITY(1, 1) NOT NULL,
    [MethodName] [nvarchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
    [IsActive] [bit] NOT NULL,
    CONSTRAINT [PK_PaymentMethod] PRIMARY KEY CLUSTERED
    (
        [PaymentMethodId] ASC
    ) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY];
GO
ALTER TABLE [dbo].[PaymentMethod] ADD CONSTRAINT [DF_PaymentMethod_IsActive] DEFAULT ((1)) FOR [IsActive];
GO

-- ----------------------------------------------------------------------------
-- Table: [dbo].[Permission]
-- ----------------------------------------------------------------------------
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
SET ANSI_PADDING ON;
GO
CREATE TABLE [dbo].[Permission] (
    [PermissionId] [bigint] IDENTITY(1, 1) NOT NULL,
    [PermissionName] [nvarchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
    [ModuleName] [nvarchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
    [Description] [nvarchar](250) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
    CONSTRAINT [PK_Permission] PRIMARY KEY CLUSTERED
    (
        [PermissionId] ASC
    ) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY];
GO

-- ----------------------------------------------------------------------------
-- Table: [dbo].[Product]
-- ----------------------------------------------------------------------------
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
SET ANSI_PADDING ON;
GO
CREATE TABLE [dbo].[Product] (
    [ProductId] [int] IDENTITY(1, 1) NOT NULL,
    [Name] [varchar](500) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
    [Price] [varchar](500) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
    [Size] [varchar](500) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
    [IsActive] [bit] NOT NULL,
    [IsDelete] [bit] NOT NULL,
    [CreatedBY] [int] NOT NULL,
    [CreatedDate] [datetime] NOT NULL,
    [ModifiedBy] [int] NULL,
    [ModifiedDate] [datetime] NULL,
    [CategoryId] [int] NULL,
    [BrandId] [int] NULL,
    [UserId] [int] NULL,
    [ImageUrl] [nvarchar](500) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
    [Image] [nvarchar](500) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
    [ProductModelId] [bigint] NULL,
    [SKU] [nvarchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
    [Barcode] [nvarchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
    [RAM] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
    [Storage] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
    [Color] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
    [MRP] [decimal](18, 2) NULL,
    [SellingPrice] [decimal](18, 2) NULL,
    [WarrantyMonths] [int] NOT NULL,
    [TrackIMEI] [bit] NOT NULL,
    [ImagePath] [nvarchar](500) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
    CONSTRAINT [PK__Product__B40CC6CDB1188448] PRIMARY KEY CLUSTERED
    (
        [ProductId] ASC
    ) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY];
GO
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET QUOTED_IDENTIFIER ON;
SET NUMERIC_ROUNDABORT OFF;
GO
CREATE UNIQUE NONCLUSTERED INDEX [UQ_Product_Barcode] ON [dbo].[Product] (
    [Barcode] ASC
)
WHERE ([Barcode] IS NOT NULL AND [Barcode]<>'' AND [IsDelete]=(0))
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY];
GO
CREATE UNIQUE NONCLUSTERED INDEX [UQ_Product_SKU] ON [dbo].[Product] (
    [SKU] ASC
)
WHERE ([SKU] IS NOT NULL AND [SKU]<>'' AND [IsDelete]=(0))
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY];
GO
ALTER TABLE [dbo].[Product] ADD CONSTRAINT [DF__Product__IsActiv__37A5467C] DEFAULT ((1)) FOR [IsActive];
ALTER TABLE [dbo].[Product] ADD CONSTRAINT [DF__Product__IsDelet__38996AB5] DEFAULT ((0)) FOR [IsDelete];
ALTER TABLE [dbo].[Product] ADD CONSTRAINT [DF_Product_CreatedDate] DEFAULT (getutcdate()) FOR [CreatedDate];
ALTER TABLE [dbo].[Product] ADD CONSTRAINT [DF_Product_TrackIMEI] DEFAULT ((0)) FOR [TrackIMEI];
ALTER TABLE [dbo].[Product] ADD CONSTRAINT [DF_Product_WarrantyMonths] DEFAULT ((0)) FOR [WarrantyMonths];
GO

-- ----------------------------------------------------------------------------
-- Table: [dbo].[ProductModel]
-- ----------------------------------------------------------------------------
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
SET ANSI_PADDING ON;
GO
CREATE TABLE [dbo].[ProductModel] (
    [ProductModelId] [bigint] IDENTITY(1, 1) NOT NULL,
    [BrandId] [int] NOT NULL,
    [ModelName] [nvarchar](150) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
    [ModelCode] [nvarchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
    [Description] [nvarchar](MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
    [IsActive] [bit] NOT NULL,
    [CreatedDate] [datetime2](7) NOT NULL,
    CONSTRAINT [PK_ProductModel] PRIMARY KEY CLUSTERED
    (
        [ProductModelId] ASC
    ) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY];
GO
ALTER TABLE [dbo].[ProductModel] ADD CONSTRAINT [DF_ProductModel_CreatedDate] DEFAULT (sysutcdatetime()) FOR [CreatedDate];
ALTER TABLE [dbo].[ProductModel] ADD CONSTRAINT [DF_ProductModel_IsActive] DEFAULT ((1)) FOR [IsActive];
GO

-- ----------------------------------------------------------------------------
-- Table: [dbo].[Role]
-- ----------------------------------------------------------------------------
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
SET ANSI_PADDING ON;
GO
CREATE TABLE [dbo].[Role] (
    [RoleId] [bigint] IDENTITY(1, 1) NOT NULL,
    [RoleName] [nvarchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
    [Description] [nvarchar](250) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
    [IsActive] [bit] NOT NULL,
    CONSTRAINT [PK_Role] PRIMARY KEY CLUSTERED
    (
        [RoleId] ASC
    ) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY];
GO
ALTER TABLE [dbo].[Role] ADD CONSTRAINT [DF_Role_IsActive] DEFAULT ((1)) FOR [IsActive];
GO

-- ----------------------------------------------------------------------------
-- Table: [dbo].[Supplier]
-- ----------------------------------------------------------------------------
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
SET ANSI_PADDING ON;
GO
CREATE TABLE [dbo].[Supplier] (
    [SupplierId] [bigint] IDENTITY(1, 1) NOT NULL,
    [SupplierName] [nvarchar](150) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
    [ContactPerson] [nvarchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
    [Phone] [nvarchar](30) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
    [Email] [nvarchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
    [GSTNumber] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
    [Address] [nvarchar](500) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
    [OpeningBalance] [decimal](18, 2) NOT NULL,
    [CurrentBalance] [decimal](18, 2) NOT NULL,
    [IsActive] [bit] NOT NULL,
    [IsDelete] [bit] NOT NULL,
    [CreatedDate] [datetime2](7) NOT NULL,
    [UserId] [int] NULL,
    CONSTRAINT [PK_Supplier] PRIMARY KEY CLUSTERED
    (
        [SupplierId] ASC
    ) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY];
GO
ALTER TABLE [dbo].[Supplier] ADD CONSTRAINT [DF_Supplier_CreatedDate] DEFAULT (sysutcdatetime()) FOR [CreatedDate];
ALTER TABLE [dbo].[Supplier] ADD CONSTRAINT [DF_Supplier_CurrentBalance] DEFAULT ((0.00)) FOR [CurrentBalance];
ALTER TABLE [dbo].[Supplier] ADD CONSTRAINT [DF_Supplier_IsActive] DEFAULT ((1)) FOR [IsActive];
ALTER TABLE [dbo].[Supplier] ADD CONSTRAINT [DF_Supplier_IsDelete] DEFAULT ((0)) FOR [IsDelete];
ALTER TABLE [dbo].[Supplier] ADD CONSTRAINT [DF_Supplier_OpeningBalance] DEFAULT ((0.00)) FOR [OpeningBalance];
GO

-- ----------------------------------------------------------------------------
-- Table: [dbo].[Users]
-- ----------------------------------------------------------------------------
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
SET ANSI_PADDING ON;
GO
CREATE TABLE [dbo].[Users] (
    [UserId] [int] IDENTITY(1, 1) NOT NULL,
    [Username] [nvarchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
    [UserPassword] [nvarchar](255) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
    [MobileNumber] [nvarchar](20) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
    [IsDelete] [bit] NOT NULL,
    [CreatedDate] [datetime] NOT NULL,
    [LastLoginDate] [datetime] NULL,
    CONSTRAINT [PK__Users__1788CC4C3DD79548] PRIMARY KEY CLUSTERED
    (
        [UserId] ASC
    ) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY],
    CONSTRAINT [UQ__Users__536C85E4A1BDBAAD] UNIQUE NONCLUSTERED
    (
        [Username] ASC
    ) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY];
GO
ALTER TABLE [dbo].[Users] ADD CONSTRAINT [DF__Users__CreatedDa__72C60C4A] DEFAULT (getdate()) FOR [CreatedDate];
ALTER TABLE [dbo].[Users] ADD CONSTRAINT [DF__Users__IsDelete__71D1E811] DEFAULT ((0)) FOR [IsDelete];
GO

-- ----------------------------------------------------------------------------
-- Table: [dbo].[Branch]
-- ----------------------------------------------------------------------------
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
SET ANSI_PADDING ON;
GO
CREATE TABLE [dbo].[Branch] (
    [BranchId] [bigint] IDENTITY(1, 1) NOT NULL,
    [CompanyId] [bigint] NOT NULL,
    [BranchName] [nvarchar](150) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
    [BranchCode] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
    [Address] [nvarchar](500) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
    [Phone] [nvarchar](30) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
    [Email] [nvarchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
    [IsActive] [bit] NOT NULL,
    [CreatedDate] [datetime2](7) NOT NULL,
    CONSTRAINT [PK_Branch] PRIMARY KEY CLUSTERED
    (
        [BranchId] ASC
    ) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY];
GO
ALTER TABLE [dbo].[Branch] ADD CONSTRAINT [DF_Branch_CreatedDate] DEFAULT (sysutcdatetime()) FOR [CreatedDate];
ALTER TABLE [dbo].[Branch] ADD CONSTRAINT [DF_Branch_IsActive] DEFAULT ((1)) FOR [IsActive];
GO

-- ----------------------------------------------------------------------------
-- Table: [dbo].[ProductSpecification]
-- ----------------------------------------------------------------------------
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
SET ANSI_PADDING ON;
GO
CREATE TABLE [dbo].[ProductSpecification] (
    [SpecificationId] [bigint] IDENTITY(1, 1) NOT NULL,
    [ProductModelId] [bigint] NOT NULL,
    [SpecificationName] [nvarchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
    [SpecificationValue] [nvarchar](500) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
    CONSTRAINT [PK_ProductSpecification] PRIMARY KEY CLUSTERED
    (
        [SpecificationId] ASC
    ) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY];
GO

-- ----------------------------------------------------------------------------
-- Table: [dbo].[RolePermission]
-- ----------------------------------------------------------------------------
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
CREATE TABLE [dbo].[RolePermission] (
    [RolePermissionId] [bigint] IDENTITY(1, 1) NOT NULL,
    [RoleId] [bigint] NOT NULL,
    [PermissionId] [bigint] NOT NULL,
    [CanView] [bit] NOT NULL,
    [CanAdd] [bit] NOT NULL,
    [CanEdit] [bit] NOT NULL,
    [CanDelete] [bit] NOT NULL,
    CONSTRAINT [PK_RolePermission] PRIMARY KEY CLUSTERED
    (
        [RolePermissionId] ASC
    ) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY];
GO
ALTER TABLE [dbo].[RolePermission] ADD CONSTRAINT [DF_RolePermission_CanAdd] DEFAULT ((0)) FOR [CanAdd];
ALTER TABLE [dbo].[RolePermission] ADD CONSTRAINT [DF_RolePermission_CanDelete] DEFAULT ((0)) FOR [CanDelete];
ALTER TABLE [dbo].[RolePermission] ADD CONSTRAINT [DF_RolePermission_CanEdit] DEFAULT ((0)) FOR [CanEdit];
ALTER TABLE [dbo].[RolePermission] ADD CONSTRAINT [DF_RolePermission_CanView] DEFAULT ((0)) FOR [CanView];
GO

-- ----------------------------------------------------------------------------
-- Table: [dbo].[UserRole]
-- ----------------------------------------------------------------------------
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
CREATE TABLE [dbo].[UserRole] (
    [UserRoleId] [bigint] IDENTITY(1, 1) NOT NULL,
    [UserId] [int] NOT NULL,
    [RoleId] [bigint] NOT NULL,
    CONSTRAINT [PK_UserRole] PRIMARY KEY CLUSTERED
    (
        [UserRoleId] ASC
    ) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY];
GO

-- ----------------------------------------------------------------------------
-- Table: [dbo].[Expense]
-- ----------------------------------------------------------------------------
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
CREATE TABLE [dbo].[Expense] (
    [ExpenseId] [bigint] IDENTITY(1, 1) NOT NULL,
    [ExpenseCategoryId] [int] NOT NULL,
    [BranchId] [bigint] NOT NULL,
    [ExpenseDate] [datetime2](7) NOT NULL,
    [Amount] [decimal](18, 2) NOT NULL,
    [Description] [nvarchar](500) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
    [PaymentMethodId] [int] NOT NULL,
    [CreatedBy] [int] NULL,
    [CreatedDate] [datetime2](7) NOT NULL,
    [UserId] [int] NULL,
    CONSTRAINT [PK_Expense] PRIMARY KEY CLUSTERED
    (
        [ExpenseId] ASC
    ) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY];
GO
ALTER TABLE [dbo].[Expense] ADD CONSTRAINT [DF_Expense_CreatedDate] DEFAULT (sysutcdatetime()) FOR [CreatedDate];
ALTER TABLE [dbo].[Expense] ADD CONSTRAINT [DF_Expense_Date] DEFAULT (sysutcdatetime()) FOR [ExpenseDate];
GO

-- ----------------------------------------------------------------------------
-- Table: [dbo].[Purchase]
-- ----------------------------------------------------------------------------
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
SET ANSI_PADDING ON;
GO
CREATE TABLE [dbo].[Purchase] (
    [PurchaseId] [bigint] IDENTITY(1, 1) NOT NULL,
    [SupplierId] [bigint] NOT NULL,
    [BranchId] [bigint] NOT NULL,
    [InvoiceNumber] [nvarchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
    [PurchaseDate] [datetime2](7) NOT NULL,
    [SubTotal] [decimal](18, 2) NOT NULL,
    [Discount] [decimal](18, 2) NOT NULL,
    [TaxAmount] [decimal](18, 2) NOT NULL,
    [GrandTotal] [decimal](18, 2) NOT NULL,
    [PaidAmount] [decimal](18, 2) NOT NULL,
    [DueAmount] [decimal](18, 2) NOT NULL,
    [Status] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
    [Notes] [nvarchar](500) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
    [CreatedBy] [int] NULL,
    [CreatedDate] [datetime2](7) NOT NULL,
    [UserId] [int] NULL,
    CONSTRAINT [PK_Purchase] PRIMARY KEY CLUSTERED
    (
        [PurchaseId] ASC
    ) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY];
GO
ALTER TABLE [dbo].[Purchase] ADD CONSTRAINT [DF_Purchase_CreatedDate] DEFAULT (sysutcdatetime()) FOR [CreatedDate];
ALTER TABLE [dbo].[Purchase] ADD CONSTRAINT [DF_Purchase_Discount] DEFAULT ((0.00)) FOR [Discount];
ALTER TABLE [dbo].[Purchase] ADD CONSTRAINT [DF_Purchase_DueAmount] DEFAULT ((0.00)) FOR [DueAmount];
ALTER TABLE [dbo].[Purchase] ADD CONSTRAINT [DF_Purchase_PaidAmount] DEFAULT ((0.00)) FOR [PaidAmount];
ALTER TABLE [dbo].[Purchase] ADD CONSTRAINT [DF_Purchase_Status] DEFAULT ('Received') FOR [Status];
ALTER TABLE [dbo].[Purchase] ADD CONSTRAINT [DF_Purchase_TaxAmount] DEFAULT ((0.00)) FOR [TaxAmount];
GO

-- ----------------------------------------------------------------------------
-- Table: [dbo].[SalesInvoice]
-- ----------------------------------------------------------------------------
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
SET ANSI_PADDING ON;
GO
CREATE TABLE [dbo].[SalesInvoice] (
    [SalesInvoiceId] [bigint] IDENTITY(1, 1) NOT NULL,
    [BranchId] [bigint] NOT NULL,
    [CustomerId] [bigint] NULL,
    [InvoiceNumber] [nvarchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
    [InvoiceDate] [datetime2](7) NOT NULL,
    [SubTotal] [decimal](18, 2) NOT NULL,
    [Discount] [decimal](18, 2) NOT NULL,
    [TaxAmount] [decimal](18, 2) NOT NULL,
    [GrandTotal] [decimal](18, 2) NOT NULL,
    [PaidAmount] [decimal](18, 2) NOT NULL,
    [DueAmount] [decimal](18, 2) NOT NULL,
    [Status] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
    [Notes] [nvarchar](500) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
    [CreatedBy] [int] NULL,
    [CreatedDate] [datetime2](7) NOT NULL,
    [UserId] [int] NULL,
    CONSTRAINT [PK_SalesInvoice] PRIMARY KEY CLUSTERED
    (
        [SalesInvoiceId] ASC
    ) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY],
    CONSTRAINT [UQ_SalesInvoice_Number] UNIQUE NONCLUSTERED
    (
        [InvoiceNumber] ASC
    ) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY];
GO
ALTER TABLE [dbo].[SalesInvoice] ADD CONSTRAINT [DF_SalesInvoice_CreatedDate] DEFAULT (sysutcdatetime()) FOR [CreatedDate];
ALTER TABLE [dbo].[SalesInvoice] ADD CONSTRAINT [DF_SalesInvoice_Date] DEFAULT (sysutcdatetime()) FOR [InvoiceDate];
ALTER TABLE [dbo].[SalesInvoice] ADD CONSTRAINT [DF_SalesInvoice_Discount] DEFAULT ((0.00)) FOR [Discount];
ALTER TABLE [dbo].[SalesInvoice] ADD CONSTRAINT [DF_SalesInvoice_DueAmount] DEFAULT ((0.00)) FOR [DueAmount];
ALTER TABLE [dbo].[SalesInvoice] ADD CONSTRAINT [DF_SalesInvoice_PaidAmount] DEFAULT ((0.00)) FOR [PaidAmount];
ALTER TABLE [dbo].[SalesInvoice] ADD CONSTRAINT [DF_SalesInvoice_Status] DEFAULT ('Paid') FOR [Status];
ALTER TABLE [dbo].[SalesInvoice] ADD CONSTRAINT [DF_SalesInvoice_TaxAmount] DEFAULT ((0.00)) FOR [TaxAmount];
GO

-- ----------------------------------------------------------------------------
-- Table: [dbo].[Warehouse]
-- ----------------------------------------------------------------------------
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
SET ANSI_PADDING ON;
GO
CREATE TABLE [dbo].[Warehouse] (
    [WarehouseId] [bigint] IDENTITY(1, 1) NOT NULL,
    [BranchId] [bigint] NOT NULL,
    [WarehouseName] [nvarchar](150) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
    [Location] [nvarchar](250) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
    [IsActive] [bit] NOT NULL,
    [CreatedDate] [datetime2](7) NOT NULL,
    CONSTRAINT [PK_Warehouse] PRIMARY KEY CLUSTERED
    (
        [WarehouseId] ASC
    ) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY];
GO
ALTER TABLE [dbo].[Warehouse] ADD CONSTRAINT [DF_Warehouse_CreatedDate] DEFAULT (sysutcdatetime()) FOR [CreatedDate];
ALTER TABLE [dbo].[Warehouse] ADD CONSTRAINT [DF_Warehouse_IsActive] DEFAULT ((1)) FOR [IsActive];
GO

-- ----------------------------------------------------------------------------
-- Table: [dbo].[PurchaseItem]
-- ----------------------------------------------------------------------------
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
CREATE TABLE [dbo].[PurchaseItem] (
    [PurchaseItemId] [bigint] IDENTITY(1, 1) NOT NULL,
    [PurchaseId] [bigint] NOT NULL,
    [ProductId] [int] NOT NULL,
    [Quantity] [int] NOT NULL,
    [UnitCost] [decimal](18, 2) NOT NULL,
    [Discount] [decimal](18, 2) NOT NULL,
    [TaxAmount] [decimal](18, 2) NOT NULL,
    [TotalAmount] [decimal](18, 2) NOT NULL,
    CONSTRAINT [PK_PurchaseItem] PRIMARY KEY CLUSTERED
    (
        [PurchaseItemId] ASC
    ) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY];
GO
ALTER TABLE [dbo].[PurchaseItem] ADD CONSTRAINT [DF_PurchaseItem_Discount] DEFAULT ((0.00)) FOR [Discount];
ALTER TABLE [dbo].[PurchaseItem] ADD CONSTRAINT [DF_PurchaseItem_TaxAmount] DEFAULT ((0.00)) FOR [TaxAmount];
GO

-- ----------------------------------------------------------------------------
-- Table: [dbo].[PurchasePayment]
-- ----------------------------------------------------------------------------
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
SET ANSI_PADDING ON;
GO
CREATE TABLE [dbo].[PurchasePayment] (
    [PurchasePaymentId] [bigint] IDENTITY(1, 1) NOT NULL,
    [PurchaseId] [bigint] NOT NULL,
    [PaymentMethodId] [int] NOT NULL,
    [Amount] [decimal](18, 2) NOT NULL,
    [TransactionReference] [nvarchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
    [PaymentDate] [datetime2](7) NOT NULL,
    [Notes] [nvarchar](500) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
    [CreatedBy] [int] NULL,
    CONSTRAINT [PK_PurchasePayment] PRIMARY KEY CLUSTERED
    (
        [PurchasePaymentId] ASC
    ) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY];
GO
ALTER TABLE [dbo].[PurchasePayment] ADD CONSTRAINT [DF_PurchasePayment_PaymentDate] DEFAULT (sysutcdatetime()) FOR [PaymentDate];
GO

-- ----------------------------------------------------------------------------
-- Table: [dbo].[SalesInvoiceItem]
-- ----------------------------------------------------------------------------
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
CREATE TABLE [dbo].[SalesInvoiceItem] (
    [SalesInvoiceItemId] [bigint] IDENTITY(1, 1) NOT NULL,
    [SalesInvoiceId] [bigint] NOT NULL,
    [ProductId] [int] NOT NULL,
    [Quantity] [int] NOT NULL,
    [UnitPrice] [decimal](18, 2) NOT NULL,
    [Discount] [decimal](18, 2) NOT NULL,
    [TaxAmount] [decimal](18, 2) NOT NULL,
    [TotalAmount] [decimal](18, 2) NOT NULL,
    CONSTRAINT [PK_SalesInvoiceItem] PRIMARY KEY CLUSTERED
    (
        [SalesInvoiceItemId] ASC
    ) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY];
GO
ALTER TABLE [dbo].[SalesInvoiceItem] ADD CONSTRAINT [DF_SalesInvoiceItem_Discount] DEFAULT ((0.00)) FOR [Discount];
ALTER TABLE [dbo].[SalesInvoiceItem] ADD CONSTRAINT [DF_SalesInvoiceItem_TaxAmount] DEFAULT ((0.00)) FOR [TaxAmount];
GO

-- ----------------------------------------------------------------------------
-- Table: [dbo].[SalesPayment]
-- ----------------------------------------------------------------------------
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
CREATE TABLE [dbo].[SalesPayment] (
    [SalesPaymentId] [bigint] IDENTITY(1, 1) NOT NULL,
    [SalesInvoiceId] [bigint] NOT NULL,
    [PaymentMethodId] [int] NOT NULL,
    [Amount] [decimal](18, 2) NOT NULL,
    [TransactionReference] [nvarchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
    [PaymentDate] [datetime2](7) NOT NULL,
    [CreatedBy] [int] NULL,
    CONSTRAINT [PK_SalesPayment] PRIMARY KEY CLUSTERED
    (
        [SalesPaymentId] ASC
    ) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY];
GO
ALTER TABLE [dbo].[SalesPayment] ADD CONSTRAINT [DF_SalesPayment_Date] DEFAULT (sysutcdatetime()) FOR [PaymentDate];
GO

-- ----------------------------------------------------------------------------
-- Table: [dbo].[SalesReturn]
-- ----------------------------------------------------------------------------
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
SET ANSI_PADDING ON;
GO
CREATE TABLE [dbo].[SalesReturn] (
    [SalesReturnId] [bigint] IDENTITY(1, 1) NOT NULL,
    [SalesInvoiceId] [bigint] NOT NULL,
    [CustomerId] [bigint] NULL,
    [ReturnDate] [datetime2](7) NOT NULL,
    [Reason] [nvarchar](500) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
    [RefundAmount] [decimal](18, 2) NOT NULL,
    [Status] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
    [CreatedBy] [int] NULL,
    [CreatedDate] [datetime2](7) NOT NULL,
    CONSTRAINT [PK_SalesReturn] PRIMARY KEY CLUSTERED
    (
        [SalesReturnId] ASC
    ) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY];
GO
ALTER TABLE [dbo].[SalesReturn] ADD CONSTRAINT [DF_SalesReturn_CreatedDate] DEFAULT (sysutcdatetime()) FOR [CreatedDate];
ALTER TABLE [dbo].[SalesReturn] ADD CONSTRAINT [DF_SalesReturn_Date] DEFAULT (sysutcdatetime()) FOR [ReturnDate];
ALTER TABLE [dbo].[SalesReturn] ADD CONSTRAINT [DF_SalesReturn_Refund] DEFAULT ((0.00)) FOR [RefundAmount];
ALTER TABLE [dbo].[SalesReturn] ADD CONSTRAINT [DF_SalesReturn_Status] DEFAULT ('Approved') FOR [Status];
GO

-- ----------------------------------------------------------------------------
-- Table: [dbo].[StockBalance]
-- ----------------------------------------------------------------------------
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
CREATE TABLE [dbo].[StockBalance] (
    [StockBalanceId] [bigint] IDENTITY(1, 1) NOT NULL,
    [ProductId] [int] NOT NULL,
    [WarehouseId] [bigint] NOT NULL,
    [QuantityOnHand] [int] NOT NULL,
    [QuantityReserved] [int] NOT NULL,
    [ReorderLevel] [int] NOT NULL,
    [LastUpdated] [datetime2](7) NOT NULL,
    CONSTRAINT [PK_StockBalance] PRIMARY KEY CLUSTERED
    (
        [StockBalanceId] ASC
    ) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY],
    CONSTRAINT [UQ_StockBalance_Product_Warehouse] UNIQUE NONCLUSTERED
    (
        [ProductId] ASC,
        [WarehouseId] ASC
    ) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY];
GO
ALTER TABLE [dbo].[StockBalance] ADD CONSTRAINT [DF_StockBalance_LastUpdated] DEFAULT (sysutcdatetime()) FOR [LastUpdated];
ALTER TABLE [dbo].[StockBalance] ADD CONSTRAINT [DF_StockBalance_OnHand] DEFAULT ((0)) FOR [QuantityOnHand];
ALTER TABLE [dbo].[StockBalance] ADD CONSTRAINT [DF_StockBalance_Reorder] DEFAULT ((5)) FOR [ReorderLevel];
ALTER TABLE [dbo].[StockBalance] ADD CONSTRAINT [DF_StockBalance_Reserved] DEFAULT ((0)) FOR [QuantityReserved];
GO

-- ----------------------------------------------------------------------------
-- Table: [dbo].[StockMovement]
-- ----------------------------------------------------------------------------
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
SET ANSI_PADDING ON;
GO
CREATE TABLE [dbo].[StockMovement] (
    [StockMovementId] [bigint] IDENTITY(1, 1) NOT NULL,
    [WarehouseId] [bigint] NOT NULL,
    [MovementType] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
    [ReferenceType] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
    [ReferenceId] [bigint] NOT NULL,
    [MovementDate] [datetime2](7) NOT NULL,
    [Notes] [nvarchar](500) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
    [CreatedBy] [int] NULL,
    CONSTRAINT [PK_StockMovement] PRIMARY KEY CLUSTERED
    (
        [StockMovementId] ASC
    ) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY];
GO
ALTER TABLE [dbo].[StockMovement] ADD CONSTRAINT [DF_StockMovement_Date] DEFAULT (sysutcdatetime()) FOR [MovementDate];
GO

-- ----------------------------------------------------------------------------
-- Table: [dbo].[DeviceUnit]
-- ----------------------------------------------------------------------------
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
SET ANSI_PADDING ON;
GO
CREATE TABLE [dbo].[DeviceUnit] (
    [DeviceUnitId] [bigint] IDENTITY(1, 1) NOT NULL,
    [ProductId] [int] NOT NULL,
    [PurchaseItemId] [bigint] NULL,
    [IMEI1] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
    [IMEI2] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
    [SerialNumber] [nvarchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
    [WarehouseId] [bigint] NOT NULL,
    [CurrentStatus] [int] NOT NULL,
    [WarrantyStartDate] [datetime2](7) NULL,
    [WarrantyEndDate] [datetime2](7) NULL,
    [CreatedDate] [datetime2](7) NOT NULL,
    [CostPrice] [decimal](18, 2) NULL,
    [Notes] [nvarchar](500) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
    CONSTRAINT [PK_DeviceUnit] PRIMARY KEY CLUSTERED
    (
        [DeviceUnitId] ASC
    ) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY];
GO
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET QUOTED_IDENTIFIER ON;
SET NUMERIC_ROUNDABORT OFF;
GO
CREATE UNIQUE NONCLUSTERED INDEX [UQ_DeviceUnit_IMEI1] ON [dbo].[DeviceUnit] (
    [IMEI1] ASC
)
WHERE ([IMEI1] IS NOT NULL AND [IMEI1]<>'')
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY];
GO
CREATE UNIQUE NONCLUSTERED INDEX [UQ_DeviceUnit_IMEI2] ON [dbo].[DeviceUnit] (
    [IMEI2] ASC
)
WHERE ([IMEI2] IS NOT NULL AND [IMEI2]<>'')
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY];
GO
ALTER TABLE [dbo].[DeviceUnit] ADD CONSTRAINT [DF_DeviceUnit_CreatedDate] DEFAULT (sysutcdatetime()) FOR [CreatedDate];
ALTER TABLE [dbo].[DeviceUnit] ADD CONSTRAINT [DF_DeviceUnit_CurrentStatus] DEFAULT ((1)) FOR [CurrentStatus];
GO

-- ----------------------------------------------------------------------------
-- Table: [dbo].[SalesExchange]
-- ----------------------------------------------------------------------------
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
CREATE TABLE [dbo].[SalesExchange] (
    [ExchangeId] [bigint] IDENTITY(1, 1) NOT NULL,
    [SalesReturnId] [bigint] NOT NULL,
    [NewSalesInvoiceId] [bigint] NOT NULL,
    [ExchangeDifference] [decimal](18, 2) NOT NULL,
    [ExchangeDate] [datetime2](7) NOT NULL,
    CONSTRAINT [PK_SalesExchange] PRIMARY KEY CLUSTERED
    (
        [ExchangeId] ASC
    ) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY];
GO
ALTER TABLE [dbo].[SalesExchange] ADD CONSTRAINT [DF_SalesExchange_Date] DEFAULT (sysutcdatetime()) FOR [ExchangeDate];
GO

-- ----------------------------------------------------------------------------
-- Table: [dbo].[SalesReturnItem]
-- ----------------------------------------------------------------------------
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
CREATE TABLE [dbo].[SalesReturnItem] (
    [SalesReturnItemId] [bigint] IDENTITY(1, 1) NOT NULL,
    [SalesReturnId] [bigint] NOT NULL,
    [SalesInvoiceItemId] [bigint] NULL,
    [ProductId] [int] NOT NULL,
    [Quantity] [int] NOT NULL,
    [ReturnCondition] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
    [RefundAmount] [decimal](18, 2) NOT NULL,
    CONSTRAINT [PK_SalesReturnItem] PRIMARY KEY CLUSTERED
    (
        [SalesReturnItemId] ASC
    ) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY];
GO
ALTER TABLE [dbo].[SalesReturnItem] ADD CONSTRAINT [DF_SalesReturnItem_Condition] DEFAULT ('Good') FOR [ReturnCondition];
GO

-- ----------------------------------------------------------------------------
-- Table: [dbo].[SalesReturnPayment]
-- ----------------------------------------------------------------------------
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
CREATE TABLE [dbo].[SalesReturnPayment] (
    [ReturnPaymentId] [bigint] IDENTITY(1, 1) NOT NULL,
    [SalesReturnId] [bigint] NOT NULL,
    [PaymentMethodId] [int] NOT NULL,
    [Amount] [decimal](18, 2) NOT NULL,
    [PaymentDate] [datetime2](7) NOT NULL,
    [CreatedBy] [int] NULL,
    CONSTRAINT [PK_SalesReturnPayment] PRIMARY KEY CLUSTERED
    (
        [ReturnPaymentId] ASC
    ) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY];
GO
ALTER TABLE [dbo].[SalesReturnPayment] ADD CONSTRAINT [DF_SalesReturnPayment_Date] DEFAULT (sysutcdatetime()) FOR [PaymentDate];
GO

-- ----------------------------------------------------------------------------
-- Table: [dbo].[StockMovementItem]
-- ----------------------------------------------------------------------------
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
CREATE TABLE [dbo].[StockMovementItem] (
    [StockMovementItemId] [bigint] IDENTITY(1, 1) NOT NULL,
    [StockMovementId] [bigint] NOT NULL,
    [ProductId] [int] NOT NULL,
    [QuantityIn] [int] NOT NULL,
    [QuantityOut] [int] NOT NULL,
    [UnitCost] [decimal](18, 2) NOT NULL,
    CONSTRAINT [PK_StockMovementItem] PRIMARY KEY CLUSTERED
    (
        [StockMovementItemId] ASC
    ) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY];
GO
ALTER TABLE [dbo].[StockMovementItem] ADD CONSTRAINT [DF_StockMovementItem_In] DEFAULT ((0)) FOR [QuantityIn];
ALTER TABLE [dbo].[StockMovementItem] ADD CONSTRAINT [DF_StockMovementItem_Out] DEFAULT ((0)) FOR [QuantityOut];
ALTER TABLE [dbo].[StockMovementItem] ADD CONSTRAINT [DF_StockMovementItem_UnitCost] DEFAULT ((0.00)) FOR [UnitCost];
GO

-- ----------------------------------------------------------------------------
-- Table: [dbo].[SalesItemSerial]
-- ----------------------------------------------------------------------------
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
CREATE TABLE [dbo].[SalesItemSerial] (
    [SalesItemSerialId] [bigint] IDENTITY(1, 1) NOT NULL,
    [SalesInvoiceItemId] [bigint] NOT NULL,
    [DeviceUnitId] [bigint] NOT NULL,
    [IsReturned] [bit] NOT NULL,
    CONSTRAINT [PK_SalesItemSerial] PRIMARY KEY CLUSTERED
    (
        [SalesItemSerialId] ASC
    ) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY];
GO
ALTER TABLE [dbo].[SalesItemSerial] ADD CONSTRAINT [DF_SalesItemSerial_IsReturned] DEFAULT ((0)) FOR [IsReturned];
GO

-- ----------------------------------------------------------------------------
-- Table: [dbo].[SalesReturnSerial]
-- ----------------------------------------------------------------------------
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
CREATE TABLE [dbo].[SalesReturnSerial] (
    [ReturnSerialId] [bigint] IDENTITY(1, 1) NOT NULL,
    [SalesReturnItemId] [bigint] NOT NULL,
    [DeviceUnitId] [bigint] NOT NULL,
    CONSTRAINT [PK_SalesReturnSerial] PRIMARY KEY CLUSTERED
    (
        [ReturnSerialId] ASC
    ) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY];
GO

-- ----------------------------------------------------------------------------
-- Table: [dbo].[StockMovementSerial]
-- ----------------------------------------------------------------------------
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
CREATE TABLE [dbo].[StockMovementSerial] (
    [MovementSerialId] [bigint] IDENTITY(1, 1) NOT NULL,
    [StockMovementItemId] [bigint] NOT NULL,
    [DeviceUnitId] [bigint] NOT NULL,
    CONSTRAINT [PK_StockMovementSerial] PRIMARY KEY CLUSTERED
    (
        [MovementSerialId] ASC
    ) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY];
GO

-- ----------------------------------------------------------------------------
-- Table: [dbo].[WarrantyClaim]
-- ----------------------------------------------------------------------------
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
SET ANSI_PADDING ON;
GO
CREATE TABLE [dbo].[WarrantyClaim] (
    [WarrantyClaimId] [bigint] IDENTITY(1, 1) NOT NULL,
    [DeviceUnitId] [bigint] NOT NULL,
    [CustomerId] [bigint] NULL,
    [SalesInvoiceItemId] [bigint] NULL,
    [ClaimDate] [datetime2](7) NOT NULL,
    [IssueDescription] [nvarchar](MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
    [Status] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
    [CreatedBy] [int] NULL,
    CONSTRAINT [PK_WarrantyClaim] PRIMARY KEY CLUSTERED
    (
        [WarrantyClaimId] ASC
    ) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY];
GO
ALTER TABLE [dbo].[WarrantyClaim] ADD CONSTRAINT [DF_WarrantyClaim_Date] DEFAULT (sysutcdatetime()) FOR [ClaimDate];
ALTER TABLE [dbo].[WarrantyClaim] ADD CONSTRAINT [DF_WarrantyClaim_Status] DEFAULT ('Pending') FOR [Status];
GO

-- ----------------------------------------------------------------------------
-- Table: [dbo].[RepairJob]
-- ----------------------------------------------------------------------------
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
SET ANSI_PADDING ON;
GO
CREATE TABLE [dbo].[RepairJob] (
    [RepairJobId] [bigint] IDENTITY(1, 1) NOT NULL,
    [WarrantyClaimId] [bigint] NULL,
    [CustomerId] [bigint] NULL,
    [DeviceUnitId] [bigint] NULL,
    [DeviceModelName] [nvarchar](150) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
    [IMEIOrSerial] [nvarchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
    [ReceivedDate] [datetime2](7) NOT NULL,
    [ProblemDescription] [nvarchar](MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
    [EstimatedCost] [decimal](18, 2) NOT NULL,
    [RepairCost] [decimal](18, 2) NOT NULL,
    [Status] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
    [CompletedDate] [datetime2](7) NULL,
    [DeliveredDate] [datetime2](7) NULL,
    [TechnicianNotes] [nvarchar](MAX) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
    [CreatedBy] [int] NULL,
    CONSTRAINT [PK_RepairJob] PRIMARY KEY CLUSTERED
    (
        [RepairJobId] ASC
    ) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY];
GO
ALTER TABLE [dbo].[RepairJob] ADD CONSTRAINT [DF_RepairJob_Estimated] DEFAULT ((0.00)) FOR [EstimatedCost];
ALTER TABLE [dbo].[RepairJob] ADD CONSTRAINT [DF_RepairJob_ReceivedDate] DEFAULT (sysutcdatetime()) FOR [ReceivedDate];
ALTER TABLE [dbo].[RepairJob] ADD CONSTRAINT [DF_RepairJob_RepairCost] DEFAULT ((0.00)) FOR [RepairCost];
ALTER TABLE [dbo].[RepairJob] ADD CONSTRAINT [DF_RepairJob_Status] DEFAULT ('Received') FOR [Status];
GO

-- ----------------------------------------------------------------------------
-- Table: [dbo].[RepairJobPart]
-- ----------------------------------------------------------------------------
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
CREATE TABLE [dbo].[RepairJobPart] (
    [RepairJobPartId] [bigint] IDENTITY(1, 1) NOT NULL,
    [RepairJobId] [bigint] NOT NULL,
    [ProductId] [int] NOT NULL,
    [Quantity] [int] NOT NULL,
    [UnitCost] [decimal](18, 2) NOT NULL,
    [TotalCost] [decimal](18, 2) NOT NULL,
    CONSTRAINT [PK_RepairJobPart] PRIMARY KEY CLUSTERED
    (
        [RepairJobPartId] ASC
    ) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY];
GO
ALTER TABLE [dbo].[RepairJobPart] ADD CONSTRAINT [DF_RepairJobPart_Qty] DEFAULT ((1)) FOR [Quantity];
GO

-- ----------------------------------------------------------------------------
-- Table: [dbo].[RepairStatusHistory]
-- ----------------------------------------------------------------------------
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
SET ANSI_PADDING ON;
GO
CREATE TABLE [dbo].[RepairStatusHistory] (
    [HistoryId] [bigint] IDENTITY(1, 1) NOT NULL,
    [RepairJobId] [bigint] NOT NULL,
    [PreviousStatus] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
    [NewStatus] [nvarchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
    [Remarks] [nvarchar](500) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
    [ChangedAt] [datetime2](7) NOT NULL,
    [ChangedBy] [int] NULL,
    CONSTRAINT [PK_RepairStatusHistory] PRIMARY KEY CLUSTERED
    (
        [HistoryId] ASC
    ) WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY];
GO
ALTER TABLE [dbo].[RepairStatusHistory] ADD CONSTRAINT [DF_RepairHistory_ChangedAt] DEFAULT (sysutcdatetime()) FOR [ChangedAt];
GO

-- ============================================================================
-- 3. INSERT ALL EXISTING TABLE DATA
-- ============================================================================
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET QUOTED_IDENTIFIER ON;
SET NUMERIC_ROUNDABORT OFF;
GO

-- Data for [dbo].[Brand] (5 row(s))
SET IDENTITY_INSERT [dbo].[Brand] ON;
INSERT INTO [dbo].[Brand] ([BrandId], [BrandName], [IsActive], [IsDelete], [CreatedDate], [UserId], [ImageUrl], [Image]) VALUES (1, 'Apple', 0, 0, CAST(N'2026-11-05T15:41:02.680' AS DATETIME), 1, N'/uploads/brands/70375f3ed46f44ef94c4ee5ff9d4db70_639294702626339479.png', N'/uploads/brands/70375f3ed46f44ef94c4ee5ff9d4db70_639294702626339479.png');
INSERT INTO [dbo].[Brand] ([BrandId], [BrandName], [IsActive], [IsDelete], [CreatedDate], [UserId], [ImageUrl], [Image]) VALUES (2, 'Samsung', 1, 0, CAST(N'2026-11-05T17:18:57.860' AS DATETIME), 1, N'/uploads/brands/d7f3dd3ad3ca4096b6cbeacc62683309_639294761378288911.jpg', N'/uploads/brands/d7f3dd3ad3ca4096b6cbeacc62683309_639294761378288911.jpg');
INSERT INTO [dbo].[Brand] ([BrandId], [BrandName], [IsActive], [IsDelete], [CreatedDate], [UserId], [ImageUrl], [Image]) VALUES (3, 'One Plus', 1, 0, CAST(N'2026-11-05T17:19:48.220' AS DATETIME), 1, N'/uploads/brands/861bb27fc67a498b94d0efa8c0d53e28_639294761881964816.png', N'/uploads/brands/861bb27fc67a498b94d0efa8c0d53e28_639294761881964816.png');
INSERT INTO [dbo].[Brand] ([BrandId], [BrandName], [IsActive], [IsDelete], [CreatedDate], [UserId], [ImageUrl], [Image]) VALUES (4, 'Vivo', 1, 0, CAST(N'2026-11-05T17:20:08.963' AS DATETIME), 1, N'/uploads/brands/ef3fbccc679e495e8e6be8a1869a1c7d_639294762089471410.jpg', N'/uploads/brands/ef3fbccc679e495e8e6be8a1869a1c7d_639294762089471410.jpg');
INSERT INTO [dbo].[Brand] ([BrandId], [BrandName], [IsActive], [IsDelete], [CreatedDate], [UserId], [ImageUrl], [Image]) VALUES (5, 'Realme', 1, 0, CAST(N'2026-11-05T17:20:51.307' AS DATETIME), 1, N'/uploads/brands/fcf61f2c52cc4fbdaf4ebaebe503a224_639294762723829092.png', N'/uploads/brands/fcf61f2c52cc4fbdaf4ebaebe503a224_639294762723829092.png');
SET IDENTITY_INSERT [dbo].[Brand] OFF;
GO

DBCC CHECKIDENT (N'dbo.Brand', RESEED, 1001) WITH NO_INFOMSGS;
GO

-- Data for [dbo].[Category] (4 row(s))
SET IDENTITY_INSERT [dbo].[Category] ON;
INSERT INTO [dbo].[Category] ([CategoryId], [CategoryName], [IsActive], [IsDelete], [CreatedDate], [UserId], [ImageUrl], [Image]) VALUES (1, 'Smartphones', 1, 0, CAST(N'2026-11-05T15:42:15.427' AS DATETIME), 1, N'/uploads/categories/14e4dbc7583e4a4e9637eca2851937d7_639294703354028734.webp', N'/uploads/categories/14e4dbc7583e4a4e9637eca2851937d7_639294703354028734.webp');
INSERT INTO [dbo].[Category] ([CategoryId], [CategoryName], [IsActive], [IsDelete], [CreatedDate], [UserId], [ImageUrl], [Image]) VALUES (2, 'iPhones', 1, 0, CAST(N'2026-11-05T17:26:20.420' AS DATETIME), 1, N'/uploads/categories/8afd7c36a37c4222b0f688c6cfdccc78_639294765799812932.jpg', N'/uploads/categories/8afd7c36a37c4222b0f688c6cfdccc78_639294765799812932.jpg');
INSERT INTO [dbo].[Category] ([CategoryId], [CategoryName], [IsActive], [IsDelete], [CreatedDate], [UserId], [ImageUrl], [Image]) VALUES (3, 'Gaming Smartphones', 1, 0, CAST(N'2026-11-05T17:29:24.547' AS DATETIME), 1, N'/uploads/categories/3a8a10c24eb1448082452d55f0407191_639294767645267385.jpg', N'/uploads/categories/3a8a10c24eb1448082452d55f0407191_639294767645267385.jpg');
INSERT INTO [dbo].[Category] ([CategoryId], [CategoryName], [IsActive], [IsDelete], [CreatedDate], [UserId], [ImageUrl], [Image]) VALUES (4, 'Foldable Smartphones', 1, 0, CAST(N'2026-11-05T17:30:25.820' AS DATETIME), 1, N'/uploads/categories/eea1eb0fe2b143219aacb6681c50aaaf_639294769245625752.webp', N'/uploads/categories/eea1eb0fe2b143219aacb6681c50aaaf_639294769245625752.webp');
SET IDENTITY_INSERT [dbo].[Category] OFF;
GO

DBCC CHECKIDENT (N'dbo.Category', RESEED, 1001) WITH NO_INFOMSGS;
GO

-- Data for [dbo].[Company] (1 row(s))
SET IDENTITY_INSERT [dbo].[Company] ON;
INSERT INTO [dbo].[Company] ([CompanyId], [CompanyName], [GSTNumber], [Address], [Phone], [Email], [LogoPath], [IsActive], [CreatedDate]) VALUES (1, N'SmartCell Mobile Store', N'27AABCU9603R1ZM', N'Shop No. 12, Main Market Road', N'+91 98765 43210', N'support@smartcell.com', N'/images/logo.png', 1, CAST(N'2026-11-05T10:29:46.6058036' AS DATETIME2(7)));
SET IDENTITY_INSERT [dbo].[Company] OFF;
GO

DBCC CHECKIDENT (N'dbo.Company', RESEED, 1) WITH NO_INFOMSGS;
GO

-- Data for [dbo].[Customer] (2 row(s))
SET IDENTITY_INSERT [dbo].[Customer] ON;
INSERT INTO [dbo].[Customer] ([CustomerId], [CustomerName], [Phone], [Email], [GSTNumber], [Address], [OpeningBalance], [CurrentBalance], [IsActive], [IsDelete], [CreatedDate], [UserId]) VALUES (1, N'Vijay Sharma', N'989801122', N'vijay.sharma@example.com', N'24AAAPS9876Q1Z2', N'12, Shanti Kunj Society, Satellite, Ahmedabad', 20.00, 0.00, 1, 0, CAST(N'2026-11-05T16:25:10.3800000' AS DATETIME2(7)), 1);
INSERT INTO [dbo].[Customer] ([CustomerId], [CustomerName], [Phone], [Email], [GSTNumber], [Address], [OpeningBalance], [CurrentBalance], [IsActive], [IsDelete], [CreatedDate], [UserId]) VALUES (3, N'Umesh', N'9876543210', N'umesh23@gmail.com', NULL, NULL, 0.00, 0.00, 1, 0, CAST(N'2026-11-05T17:02:10.1666667' AS DATETIME2(7)), 1);
SET IDENTITY_INSERT [dbo].[Customer] OFF;
GO

DBCC CHECKIDENT (N'dbo.Customer', RESEED, 10001) WITH NO_INFOMSGS;
GO

-- Data for [dbo].[ExpenseCategory] (8 row(s))
SET IDENTITY_INSERT [dbo].[ExpenseCategory] ON;
INSERT INTO [dbo].[ExpenseCategory] ([ExpenseCategoryId], [CategoryName], [IsActive]) VALUES (1, N'Shop Rent', 1);
INSERT INTO [dbo].[ExpenseCategory] ([ExpenseCategoryId], [CategoryName], [IsActive]) VALUES (2, N'Electricity Bill', 1);
INSERT INTO [dbo].[ExpenseCategory] ([ExpenseCategoryId], [CategoryName], [IsActive]) VALUES (3, N'Staff Salary', 1);
INSERT INTO [dbo].[ExpenseCategory] ([ExpenseCategoryId], [CategoryName], [IsActive]) VALUES (4, N'Internet & Telephony', 1);
INSERT INTO [dbo].[ExpenseCategory] ([ExpenseCategoryId], [CategoryName], [IsActive]) VALUES (5, N'Transportation & Courier', 1);
INSERT INTO [dbo].[ExpenseCategory] ([ExpenseCategoryId], [CategoryName], [IsActive]) VALUES (6, N'Packaging & Bags', 1);
INSERT INTO [dbo].[ExpenseCategory] ([ExpenseCategoryId], [CategoryName], [IsActive]) VALUES (7, N'Office Supplies & Tea', 1);
INSERT INTO [dbo].[ExpenseCategory] ([ExpenseCategoryId], [CategoryName], [IsActive]) VALUES (8, N'Equipment Maintenance', 1);
SET IDENTITY_INSERT [dbo].[ExpenseCategory] OFF;
GO

DBCC CHECKIDENT (N'dbo.ExpenseCategory', RESEED, 1001) WITH NO_INFOMSGS;
GO

-- Data for [dbo].[PaymentMethod] (8 row(s))
SET IDENTITY_INSERT [dbo].[PaymentMethod] ON;
INSERT INTO [dbo].[PaymentMethod] ([PaymentMethodId], [MethodName], [IsActive]) VALUES (1, N'Cash', 1);
INSERT INTO [dbo].[PaymentMethod] ([PaymentMethodId], [MethodName], [IsActive]) VALUES (2, N'UPI', 1);
INSERT INTO [dbo].[PaymentMethod] ([PaymentMethodId], [MethodName], [IsActive]) VALUES (3, N'Google Pay', 1);
INSERT INTO [dbo].[PaymentMethod] ([PaymentMethodId], [MethodName], [IsActive]) VALUES (4, N'PhonePe', 1);
INSERT INTO [dbo].[PaymentMethod] ([PaymentMethodId], [MethodName], [IsActive]) VALUES (5, N'Paytm', 1);
INSERT INTO [dbo].[PaymentMethod] ([PaymentMethodId], [MethodName], [IsActive]) VALUES (6, N'Credit/Debit Card', 1);
INSERT INTO [dbo].[PaymentMethod] ([PaymentMethodId], [MethodName], [IsActive]) VALUES (7, N'Bank Transfer', 1);
INSERT INTO [dbo].[PaymentMethod] ([PaymentMethodId], [MethodName], [IsActive]) VALUES (8, N'Customer Credit', 1);
SET IDENTITY_INSERT [dbo].[PaymentMethod] OFF;
GO

DBCC CHECKIDENT (N'dbo.PaymentMethod', RESEED, 1001) WITH NO_INFOMSGS;
GO

-- Data for [dbo].[Product] (2 row(s))
SET IDENTITY_INSERT [dbo].[Product] ON;
INSERT INTO [dbo].[Product] ([ProductId], [Name], [Price], [Size], [IsActive], [IsDelete], [CreatedBY], [CreatedDate], [ModifiedBy], [ModifiedDate], [CategoryId], [BrandId], [UserId], [ImageUrl], [Image], [ProductModelId], [SKU], [Barcode], [RAM], [Storage], [Color], [MRP], [SellingPrice], [WarrantyMonths], [TrackIMEI], [ImagePath]) VALUES (1, 'iphone 16 ', '89900', '', 1, 0, 1, CAST(N'2026-11-05T15:44:34.920' AS DATETIME), NULL, NULL, 1, 1, 1, N'/uploads/products/a043fb71734d481896a96112b35ef5ee_639294704718369161.webp', N'/uploads/products/a043fb71734d481896a96112b35ef5ee_639294704718369161.webp', NULL, N'APL-IP16-256-BLU', N'8901234567890', N'8 GB', N'256 GB', N'Ultramarine Blue', 89900.00, 89900.00, 12, 1, NULL);
INSERT INTO [dbo].[Product] ([ProductId], [Name], [Price], [Size], [IsActive], [IsDelete], [CreatedBY], [CreatedDate], [ModifiedBy], [ModifiedDate], [CategoryId], [BrandId], [UserId], [ImageUrl], [Image], [ProductModelId], [SKU], [Barcode], [RAM], [Storage], [Color], [MRP], [SellingPrice], [WarrantyMonths], [TrackIMEI], [ImagePath]) VALUES (2, 'One Plus Nord CE 3  Lite 5g', '20000', '', 1, 0, 1, CAST(N'2026-11-05T17:42:41.310' AS DATETIME), NULL, NULL, 1, 3, 1, N'/uploads/products/5aa1992b596947e195b855d8ffa8dcda_639294775612739407.webp', N'/uploads/products/5aa1992b596947e195b855d8ffa8dcda_639294775612739407.webp', NULL, NULL, NULL, N'8 GB', N'128 GB', N'Pastel Lime', 20000.00, 20000.00, 12, 1, N'/uploads/products/5aa1992b596947e195b855d8ffa8dcda_639294775612739407.webp');
SET IDENTITY_INSERT [dbo].[Product] OFF;
GO

DBCC CHECKIDENT (N'dbo.Product', RESEED, 1001) WITH NO_INFOMSGS;
GO

-- Data for [dbo].[Role] (6 row(s))
SET IDENTITY_INSERT [dbo].[Role] ON;
INSERT INTO [dbo].[Role] ([RoleId], [RoleName], [Description], [IsActive]) VALUES (1, N'Super Admin', N'Full system access and master configuration', 1);
INSERT INTO [dbo].[Role] ([RoleId], [RoleName], [Description], [IsActive]) VALUES (2, N'Shop Owner', N'Owner level access to all branches, financials and reports', 1);
INSERT INTO [dbo].[Role] ([RoleId], [RoleName], [Description], [IsActive]) VALUES (3, N'Store Manager', N'Daily operational manager for sales, purchases and repairs', 1);
INSERT INTO [dbo].[Role] ([RoleId], [RoleName], [Description], [IsActive]) VALUES (4, N'Sales Executive', N'POS billing, sales invoices and IMEI validation', 1);
INSERT INTO [dbo].[Role] ([RoleId], [RoleName], [Description], [IsActive]) VALUES (5, N'Technician', N'Service, warranty and phone repair jobs', 1);
INSERT INTO [dbo].[Role] ([RoleId], [RoleName], [Description], [IsActive]) VALUES (6, N'Accountant', N'Purchases, expenses and financial reports', 1);
SET IDENTITY_INSERT [dbo].[Role] OFF;
GO

DBCC CHECKIDENT (N'dbo.Role', RESEED, 10001) WITH NO_INFOMSGS;
GO

-- Data for [dbo].[Supplier] (3 row(s))
SET IDENTITY_INSERT [dbo].[Supplier] ON;
INSERT INTO [dbo].[Supplier] ([SupplierId], [SupplierName], [ContactPerson], [Phone], [Email], [GSTNumber], [Address], [OpeningBalance], [CurrentBalance], [IsActive], [IsDelete], [CreatedDate], [UserId]) VALUES (1, N'Redington India Ltd (Apple Distributor)', N'Mahesh Patel', N'9825012345', N'orders@redington.in', N'24AAACR1234R1Z8', N'B-404, Tech Park, SG Highway, Ahmedabad', 0.00, 0.00, 1, 0, CAST(N'2026-11-05T16:25:10.3766667' AS DATETIME2(7)), 1);
INSERT INTO [dbo].[Supplier] ([SupplierId], [SupplierName], [ContactPerson], [Phone], [Email], [GSTNumber], [Address], [OpeningBalance], [CurrentBalance], [IsActive], [IsDelete], [CreatedDate], [UserId]) VALUES (2, N'Redington India Ltd (Apple Distributor)', N'Suresh Patel', N'45121221626', NULL, NULL, NULL, 20000.00, 0.00, 1, 0, CAST(N'2026-11-05T17:14:55.6466667' AS DATETIME2(7)), 1);
INSERT INTO [dbo].[Supplier] ([SupplierId], [SupplierName], [ContactPerson], [Phone], [Email], [GSTNumber], [Address], [OpeningBalance], [CurrentBalance], [IsActive], [IsDelete], [CreatedDate], [UserId]) VALUES (3, N'Gujarat LTD', N'Rajesh Kumar ', N'1238521269', N'chandu12@gmail.com', NULL, N'Anand vihar  , Delhi', 251.00, 40000.00, 1, 0, CAST(N'2026-11-05T17:46:45.9233333' AS DATETIME2(7)), 1);
SET IDENTITY_INSERT [dbo].[Supplier] OFF;
GO

DBCC CHECKIDENT (N'dbo.Supplier', RESEED, 10001) WITH NO_INFOMSGS;
GO

-- Data for [dbo].[Users] (2 row(s))
SET IDENTITY_INSERT [dbo].[Users] ON;
INSERT INTO [dbo].[Users] ([UserId], [Username], [UserPassword], [MobileNumber], [IsDelete], [CreatedDate], [LastLoginDate]) VALUES (1, N'Ashish@123', N'$2a$12$BeffaoYQA/dPtdaguK/8hOVbHe8e9l9.xsRp7z5k6leAmUQBoF/HS', N'9876543210', 0, CAST(N'2026-10-10T12:06:11.093' AS DATETIME), CAST(N'2026-11-24T10:32:41.127' AS DATETIME));
INSERT INTO [dbo].[Users] ([UserId], [Username], [UserPassword], [MobileNumber], [IsDelete], [CreatedDate], [LastLoginDate]) VALUES (2, N'Test', N'$2a$12$c6vf8UrKg1ijrjLIILT15OFfnu1wBKJhQY0nqAYguNCjs4m4RQEtm', N'8574321485', 0, CAST(N'2026-10-10T13:05:08.120' AS DATETIME), CAST(N'2026-11-24T10:32:59.580' AS DATETIME));
SET IDENTITY_INSERT [dbo].[Users] OFF;
GO

DBCC CHECKIDENT (N'dbo.Users', RESEED, 2) WITH NO_INFOMSGS;
GO

-- Data for [dbo].[Branch] (1 row(s))
SET IDENTITY_INSERT [dbo].[Branch] ON;
INSERT INTO [dbo].[Branch] ([BranchId], [CompanyId], [BranchName], [BranchCode], [Address], [Phone], [Email], [IsActive], [CreatedDate]) VALUES (1, 1, N'Main Showroom Branch', N'HQ-01', N'Shop No. 12, Main Market Road', N'+91 98765 43210', N'hq@smartcell.com', 1, CAST(N'2026-11-05T10:29:46.6125273' AS DATETIME2(7)));
SET IDENTITY_INSERT [dbo].[Branch] OFF;
GO

DBCC CHECKIDENT (N'dbo.Branch', RESEED, 1) WITH NO_INFOMSGS;
GO

-- Data for [dbo].[Expense] (6 row(s))
SET IDENTITY_INSERT [dbo].[Expense] ON;
INSERT INTO [dbo].[Expense] ([ExpenseId], [ExpenseCategoryId], [BranchId], [ExpenseDate], [Amount], [Description], [PaymentMethodId], [CreatedBy], [CreatedDate], [UserId]) VALUES (4, 2, 1, CAST(N'2026-09-03T00:00:00.0000000' AS DATETIME2(7)), 1500.00, N'Electricity Bill', 2, 1, CAST(N'2026-11-06T11:30:48.5633333' AS DATETIME2(7)), 1);
INSERT INTO [dbo].[Expense] ([ExpenseId], [ExpenseCategoryId], [BranchId], [ExpenseDate], [Amount], [Description], [PaymentMethodId], [CreatedBy], [CreatedDate], [UserId]) VALUES (10002, 2, 1, CAST(N'2026-11-03T00:00:00.0000000' AS DATETIME2(7)), 1300.00, N'Electricity  Bill', 3, 1, CAST(N'2026-11-20T18:02:09.2600000' AS DATETIME2(7)), 1);
INSERT INTO [dbo].[Expense] ([ExpenseId], [ExpenseCategoryId], [BranchId], [ExpenseDate], [Amount], [Description], [PaymentMethodId], [CreatedBy], [CreatedDate], [UserId]) VALUES (20002, 1, 1, CAST(N'2026-11-23T00:00:00.0000000' AS DATETIME2(7)), 20000.00, N'Rent for shop', 1, 1, CAST(N'2026-11-23T14:16:00.7466667' AS DATETIME2(7)), 1);
INSERT INTO [dbo].[Expense] ([ExpenseId], [ExpenseCategoryId], [BranchId], [ExpenseDate], [Amount], [Description], [PaymentMethodId], [CreatedBy], [CreatedDate], [UserId]) VALUES (20003, 4, 1, CAST(N'2026-09-10T00:00:00.0000000' AS DATETIME2(7)), 1000.00, N'Other', 1, 1, CAST(N'2026-11-23T14:16:35.5400000' AS DATETIME2(7)), 1);
INSERT INTO [dbo].[Expense] ([ExpenseId], [ExpenseCategoryId], [BranchId], [ExpenseDate], [Amount], [Description], [PaymentMethodId], [CreatedBy], [CreatedDate], [UserId]) VALUES (20004, 7, 1, CAST(N'2026-10-23T00:00:00.0000000' AS DATETIME2(7)), 500.00, N'Other', 1, 1, CAST(N'2026-11-23T14:18:02.2100000' AS DATETIME2(7)), 1);
INSERT INTO [dbo].[Expense] ([ExpenseId], [ExpenseCategoryId], [BranchId], [ExpenseDate], [Amount], [Description], [PaymentMethodId], [CreatedBy], [CreatedDate], [UserId]) VALUES (20005, 7, 1, CAST(N'2026-10-23T00:00:00.0000000' AS DATETIME2(7)), 500.00, N'Other', 1, 1, CAST(N'2026-11-23T14:18:02.2100000' AS DATETIME2(7)), 1);
SET IDENTITY_INSERT [dbo].[Expense] OFF;
GO

DBCC CHECKIDENT (N'dbo.Expense', RESEED, 30001) WITH NO_INFOMSGS;
GO

-- Data for [dbo].[Purchase] (1 row(s))
SET IDENTITY_INSERT [dbo].[Purchase] ON;
INSERT INTO [dbo].[Purchase] ([PurchaseId], [SupplierId], [BranchId], [InvoiceNumber], [PurchaseDate], [SubTotal], [Discount], [TaxAmount], [GrandTotal], [PaidAmount], [DueAmount], [Status], [Notes], [CreatedBy], [CreatedDate], [UserId]) VALUES (1, 3, 1, N'PUR-20261105-635', CAST(N'2026-11-05T00:00:00.0000000' AS DATETIME2(7)), 42000.00, 0.00, 0.00, 42000.00, 2000.00, 40000.00, N'Received', NULL, 1, CAST(N'2026-11-05T17:49:14.6133333' AS DATETIME2(7)), 1);
SET IDENTITY_INSERT [dbo].[Purchase] OFF;
GO

DBCC CHECKIDENT (N'dbo.Purchase', RESEED, 1) WITH NO_INFOMSGS;
GO

-- Data for [dbo].[Warehouse] (1 row(s))
SET IDENTITY_INSERT [dbo].[Warehouse] ON;
INSERT INTO [dbo].[Warehouse] ([WarehouseId], [BranchId], [WarehouseName], [Location], [IsActive], [CreatedDate]) VALUES (1, 1, N'Main Showroom Inventory', N'Display & Main Stock Room', 1, CAST(N'2026-11-05T10:29:46.6273446' AS DATETIME2(7)));
SET IDENTITY_INSERT [dbo].[Warehouse] OFF;
GO

DBCC CHECKIDENT (N'dbo.Warehouse', RESEED, 1) WITH NO_INFOMSGS;
GO

-- Data for [dbo].[PurchaseItem] (2 row(s))
SET IDENTITY_INSERT [dbo].[PurchaseItem] ON;
INSERT INTO [dbo].[PurchaseItem] ([PurchaseItemId], [PurchaseId], [ProductId], [Quantity], [UnitCost], [Discount], [TaxAmount], [TotalAmount]) VALUES (1, 1, 2, 2, 1000.00, 0.00, 0.00, 2000.00);
INSERT INTO [dbo].[PurchaseItem] ([PurchaseItemId], [PurchaseId], [ProductId], [Quantity], [UnitCost], [Discount], [TaxAmount], [TotalAmount]) VALUES (2, 1, 2, 2, 20000.00, 0.00, 0.00, 40000.00);
SET IDENTITY_INSERT [dbo].[PurchaseItem] OFF;
GO

DBCC CHECKIDENT (N'dbo.PurchaseItem', RESEED, 10001) WITH NO_INFOMSGS;
GO

-- Data for [dbo].[StockBalance] (2 row(s))
SET IDENTITY_INSERT [dbo].[StockBalance] ON;
INSERT INTO [dbo].[StockBalance] ([StockBalanceId], [ProductId], [WarehouseId], [QuantityOnHand], [QuantityReserved], [ReorderLevel], [LastUpdated]) VALUES (1, 1, 1, 3, 0, 5, CAST(N'2026-11-05T10:55:10.4127514' AS DATETIME2(7)));
INSERT INTO [dbo].[StockBalance] ([StockBalanceId], [ProductId], [WarehouseId], [QuantityOnHand], [QuantityReserved], [ReorderLevel], [LastUpdated]) VALUES (2, 2, 1, 4, 0, 5, CAST(N'2026-11-05T12:19:14.6401593' AS DATETIME2(7)));
SET IDENTITY_INSERT [dbo].[StockBalance] OFF;
GO

DBCC CHECKIDENT (N'dbo.StockBalance', RESEED, 10001) WITH NO_INFOMSGS;
GO

-- Data for [dbo].[DeviceUnit] (7 row(s))
SET IDENTITY_INSERT [dbo].[DeviceUnit] ON;
INSERT INTO [dbo].[DeviceUnit] ([DeviceUnitId], [ProductId], [PurchaseItemId], [IMEI1], [IMEI2], [SerialNumber], [WarehouseId], [CurrentStatus], [WarrantyStartDate], [WarrantyEndDate], [CreatedDate], [CostPrice], [Notes]) VALUES (1, 1, NULL, N'354892110293841', N'354892110293842', N'C39XYZ001APL', 1, 1, CAST(N'2026-11-05T16:25:10.4100000' AS DATETIME2(7)), NULL, CAST(N'2026-11-05T16:25:10.4100000' AS DATETIME2(7)), 78000.00, NULL);
INSERT INTO [dbo].[DeviceUnit] ([DeviceUnitId], [ProductId], [PurchaseItemId], [IMEI1], [IMEI2], [SerialNumber], [WarehouseId], [CurrentStatus], [WarrantyStartDate], [WarrantyEndDate], [CreatedDate], [CostPrice], [Notes]) VALUES (2, 1, NULL, N'354892110293843', N'354892110293844', N'C39XYZ002APL', 1, 1, CAST(N'2026-11-05T16:25:10.4100000' AS DATETIME2(7)), NULL, CAST(N'2026-11-05T16:25:10.4100000' AS DATETIME2(7)), 78000.00, NULL);
INSERT INTO [dbo].[DeviceUnit] ([DeviceUnitId], [ProductId], [PurchaseItemId], [IMEI1], [IMEI2], [SerialNumber], [WarehouseId], [CurrentStatus], [WarrantyStartDate], [WarrantyEndDate], [CreatedDate], [CostPrice], [Notes]) VALUES (3, 1, NULL, N'354892110293845', N'354892110293846', N'C39XYZ003APL', 1, 1, CAST(N'2026-11-05T16:25:10.4100000' AS DATETIME2(7)), NULL, CAST(N'2026-11-05T16:25:10.4100000' AS DATETIME2(7)), 78000.00, NULL);
INSERT INTO [dbo].[DeviceUnit] ([DeviceUnitId], [ProductId], [PurchaseItemId], [IMEI1], [IMEI2], [SerialNumber], [WarehouseId], [CurrentStatus], [WarrantyStartDate], [WarrantyEndDate], [CreatedDate], [CostPrice], [Notes]) VALUES (4, 2, 1, N'354892110293745', NULL, NULL, 1, 1, CAST(N'2026-11-05T17:49:14.6533333' AS DATETIME2(7)), NULL, CAST(N'2026-11-05T17:49:14.6533333' AS DATETIME2(7)), 1000.00, NULL);
INSERT INTO [dbo].[DeviceUnit] ([DeviceUnitId], [ProductId], [PurchaseItemId], [IMEI1], [IMEI2], [SerialNumber], [WarehouseId], [CurrentStatus], [WarrantyStartDate], [WarrantyEndDate], [CreatedDate], [CostPrice], [Notes]) VALUES (5, 2, 1, N'354894521698532', NULL, NULL, 1, 1, CAST(N'2026-11-05T17:49:14.6533333' AS DATETIME2(7)), NULL, CAST(N'2026-11-05T17:49:14.6533333' AS DATETIME2(7)), 1000.00, NULL);
INSERT INTO [dbo].[DeviceUnit] ([DeviceUnitId], [ProductId], [PurchaseItemId], [IMEI1], [IMEI2], [SerialNumber], [WarehouseId], [CurrentStatus], [WarrantyStartDate], [WarrantyEndDate], [CreatedDate], [CostPrice], [Notes]) VALUES (6, 2, 2, N'354892110293345', NULL, NULL, 1, 1, CAST(N'2026-11-05T17:49:14.6633333' AS DATETIME2(7)), NULL, CAST(N'2026-11-05T17:49:14.6633333' AS DATETIME2(7)), 20000.00, NULL);
INSERT INTO [dbo].[DeviceUnit] ([DeviceUnitId], [ProductId], [PurchaseItemId], [IMEI1], [IMEI2], [SerialNumber], [WarehouseId], [CurrentStatus], [WarrantyStartDate], [WarrantyEndDate], [CreatedDate], [CostPrice], [Notes]) VALUES (7, 2, 2, N'354892110456325', NULL, NULL, 1, 1, CAST(N'2026-11-05T17:49:14.6666667' AS DATETIME2(7)), NULL, CAST(N'2026-11-05T17:49:14.6666667' AS DATETIME2(7)), 20000.00, NULL);
SET IDENTITY_INSERT [dbo].[DeviceUnit] OFF;
GO

DBCC CHECKIDENT (N'dbo.DeviceUnit', RESEED, 10001) WITH NO_INFOMSGS;
GO

-- ============================================================================
-- 4. CREATE FOREIGN KEY CONSTRAINTS
-- ============================================================================

ALTER TABLE [dbo].[Branch] WITH CHECK ADD CONSTRAINT [FK_Branch_Company] FOREIGN KEY ([CompanyId])
REFERENCES [dbo].[Company] ([CompanyId]);
ALTER TABLE [dbo].[Branch] CHECK CONSTRAINT [FK_Branch_Company];
GO
ALTER TABLE [dbo].[DeviceUnit] WITH CHECK ADD CONSTRAINT [FK_DeviceUnit_PurchaseItem] FOREIGN KEY ([PurchaseItemId])
REFERENCES [dbo].[PurchaseItem] ([PurchaseItemId]);
ALTER TABLE [dbo].[DeviceUnit] CHECK CONSTRAINT [FK_DeviceUnit_PurchaseItem];
GO
ALTER TABLE [dbo].[DeviceUnit] WITH CHECK ADD CONSTRAINT [FK_DeviceUnit_Warehouse] FOREIGN KEY ([WarehouseId])
REFERENCES [dbo].[Warehouse] ([WarehouseId]);
ALTER TABLE [dbo].[DeviceUnit] CHECK CONSTRAINT [FK_DeviceUnit_Warehouse];
GO
ALTER TABLE [dbo].[Expense] WITH CHECK ADD CONSTRAINT [FK_Expense_Branch] FOREIGN KEY ([BranchId])
REFERENCES [dbo].[Branch] ([BranchId]);
ALTER TABLE [dbo].[Expense] CHECK CONSTRAINT [FK_Expense_Branch];
GO
ALTER TABLE [dbo].[Expense] WITH CHECK ADD CONSTRAINT [FK_Expense_Category] FOREIGN KEY ([ExpenseCategoryId])
REFERENCES [dbo].[ExpenseCategory] ([ExpenseCategoryId]);
ALTER TABLE [dbo].[Expense] CHECK CONSTRAINT [FK_Expense_Category];
GO
ALTER TABLE [dbo].[Expense] WITH CHECK ADD CONSTRAINT [FK_Expense_Method] FOREIGN KEY ([PaymentMethodId])
REFERENCES [dbo].[PaymentMethod] ([PaymentMethodId]);
ALTER TABLE [dbo].[Expense] CHECK CONSTRAINT [FK_Expense_Method];
GO
ALTER TABLE [dbo].[ProductSpecification] WITH CHECK ADD CONSTRAINT [FK_ProductSpecification_Model] FOREIGN KEY ([ProductModelId])
REFERENCES [dbo].[ProductModel] ([ProductModelId]);
ALTER TABLE [dbo].[ProductSpecification] CHECK CONSTRAINT [FK_ProductSpecification_Model];
GO
ALTER TABLE [dbo].[Purchase] WITH CHECK ADD CONSTRAINT [FK_Purchase_Branch] FOREIGN KEY ([BranchId])
REFERENCES [dbo].[Branch] ([BranchId]);
ALTER TABLE [dbo].[Purchase] CHECK CONSTRAINT [FK_Purchase_Branch];
GO
ALTER TABLE [dbo].[Purchase] WITH CHECK ADD CONSTRAINT [FK_Purchase_Supplier] FOREIGN KEY ([SupplierId])
REFERENCES [dbo].[Supplier] ([SupplierId]);
ALTER TABLE [dbo].[Purchase] CHECK CONSTRAINT [FK_Purchase_Supplier];
GO
ALTER TABLE [dbo].[PurchaseItem] WITH CHECK ADD CONSTRAINT [FK_PurchaseItem_Purchase] FOREIGN KEY ([PurchaseId])
REFERENCES [dbo].[Purchase] ([PurchaseId]) ON DELETE CASCADE;
ALTER TABLE [dbo].[PurchaseItem] CHECK CONSTRAINT [FK_PurchaseItem_Purchase];
GO
ALTER TABLE [dbo].[PurchasePayment] WITH CHECK ADD CONSTRAINT [FK_PurchasePayment_Method] FOREIGN KEY ([PaymentMethodId])
REFERENCES [dbo].[PaymentMethod] ([PaymentMethodId]);
ALTER TABLE [dbo].[PurchasePayment] CHECK CONSTRAINT [FK_PurchasePayment_Method];
GO
ALTER TABLE [dbo].[PurchasePayment] WITH CHECK ADD CONSTRAINT [FK_PurchasePayment_Purchase] FOREIGN KEY ([PurchaseId])
REFERENCES [dbo].[Purchase] ([PurchaseId]);
ALTER TABLE [dbo].[PurchasePayment] CHECK CONSTRAINT [FK_PurchasePayment_Purchase];
GO
ALTER TABLE [dbo].[RepairJob] WITH CHECK ADD CONSTRAINT [FK_RepairJob_Customer] FOREIGN KEY ([CustomerId])
REFERENCES [dbo].[Customer] ([CustomerId]);
ALTER TABLE [dbo].[RepairJob] CHECK CONSTRAINT [FK_RepairJob_Customer];
GO
ALTER TABLE [dbo].[RepairJob] WITH CHECK ADD CONSTRAINT [FK_RepairJob_Unit] FOREIGN KEY ([DeviceUnitId])
REFERENCES [dbo].[DeviceUnit] ([DeviceUnitId]);
ALTER TABLE [dbo].[RepairJob] CHECK CONSTRAINT [FK_RepairJob_Unit];
GO
ALTER TABLE [dbo].[RepairJob] WITH CHECK ADD CONSTRAINT [FK_RepairJob_Warranty] FOREIGN KEY ([WarrantyClaimId])
REFERENCES [dbo].[WarrantyClaim] ([WarrantyClaimId]);
ALTER TABLE [dbo].[RepairJob] CHECK CONSTRAINT [FK_RepairJob_Warranty];
GO
ALTER TABLE [dbo].[RepairJobPart] WITH CHECK ADD CONSTRAINT [FK_RepairJobPart_Job] FOREIGN KEY ([RepairJobId])
REFERENCES [dbo].[RepairJob] ([RepairJobId]) ON DELETE CASCADE;
ALTER TABLE [dbo].[RepairJobPart] CHECK CONSTRAINT [FK_RepairJobPart_Job];
GO
ALTER TABLE [dbo].[RepairStatusHistory] WITH CHECK ADD CONSTRAINT [FK_RepairStatusHistory_Job] FOREIGN KEY ([RepairJobId])
REFERENCES [dbo].[RepairJob] ([RepairJobId]) ON DELETE CASCADE;
ALTER TABLE [dbo].[RepairStatusHistory] CHECK CONSTRAINT [FK_RepairStatusHistory_Job];
GO
ALTER TABLE [dbo].[RolePermission] WITH CHECK ADD CONSTRAINT [FK_RolePermission_Permission] FOREIGN KEY ([PermissionId])
REFERENCES [dbo].[Permission] ([PermissionId]);
ALTER TABLE [dbo].[RolePermission] CHECK CONSTRAINT [FK_RolePermission_Permission];
GO
ALTER TABLE [dbo].[RolePermission] WITH CHECK ADD CONSTRAINT [FK_RolePermission_Role] FOREIGN KEY ([RoleId])
REFERENCES [dbo].[Role] ([RoleId]);
ALTER TABLE [dbo].[RolePermission] CHECK CONSTRAINT [FK_RolePermission_Role];
GO
ALTER TABLE [dbo].[SalesExchange] WITH CHECK ADD CONSTRAINT [FK_SalesExchange_Invoice] FOREIGN KEY ([NewSalesInvoiceId])
REFERENCES [dbo].[SalesInvoice] ([SalesInvoiceId]);
ALTER TABLE [dbo].[SalesExchange] CHECK CONSTRAINT [FK_SalesExchange_Invoice];
GO
ALTER TABLE [dbo].[SalesExchange] WITH CHECK ADD CONSTRAINT [FK_SalesExchange_Return] FOREIGN KEY ([SalesReturnId])
REFERENCES [dbo].[SalesReturn] ([SalesReturnId]);
ALTER TABLE [dbo].[SalesExchange] CHECK CONSTRAINT [FK_SalesExchange_Return];
GO
ALTER TABLE [dbo].[SalesInvoice] WITH CHECK ADD CONSTRAINT [FK_SalesInvoice_Branch] FOREIGN KEY ([BranchId])
REFERENCES [dbo].[Branch] ([BranchId]);
ALTER TABLE [dbo].[SalesInvoice] CHECK CONSTRAINT [FK_SalesInvoice_Branch];
GO
ALTER TABLE [dbo].[SalesInvoice] WITH CHECK ADD CONSTRAINT [FK_SalesInvoice_Customer] FOREIGN KEY ([CustomerId])
REFERENCES [dbo].[Customer] ([CustomerId]);
ALTER TABLE [dbo].[SalesInvoice] CHECK CONSTRAINT [FK_SalesInvoice_Customer];
GO
ALTER TABLE [dbo].[SalesInvoiceItem] WITH CHECK ADD CONSTRAINT [FK_SalesInvoiceItem_Invoice] FOREIGN KEY ([SalesInvoiceId])
REFERENCES [dbo].[SalesInvoice] ([SalesInvoiceId]) ON DELETE CASCADE;
ALTER TABLE [dbo].[SalesInvoiceItem] CHECK CONSTRAINT [FK_SalesInvoiceItem_Invoice];
GO
ALTER TABLE [dbo].[SalesItemSerial] WITH CHECK ADD CONSTRAINT [FK_SalesItemSerial_Item] FOREIGN KEY ([SalesInvoiceItemId])
REFERENCES [dbo].[SalesInvoiceItem] ([SalesInvoiceItemId]) ON DELETE CASCADE;
ALTER TABLE [dbo].[SalesItemSerial] CHECK CONSTRAINT [FK_SalesItemSerial_Item];
GO
ALTER TABLE [dbo].[SalesItemSerial] WITH CHECK ADD CONSTRAINT [FK_SalesItemSerial_Unit] FOREIGN KEY ([DeviceUnitId])
REFERENCES [dbo].[DeviceUnit] ([DeviceUnitId]);
ALTER TABLE [dbo].[SalesItemSerial] CHECK CONSTRAINT [FK_SalesItemSerial_Unit];
GO
ALTER TABLE [dbo].[SalesPayment] WITH CHECK ADD CONSTRAINT [FK_SalesPayment_Invoice] FOREIGN KEY ([SalesInvoiceId])
REFERENCES [dbo].[SalesInvoice] ([SalesInvoiceId]);
ALTER TABLE [dbo].[SalesPayment] CHECK CONSTRAINT [FK_SalesPayment_Invoice];
GO
ALTER TABLE [dbo].[SalesPayment] WITH CHECK ADD CONSTRAINT [FK_SalesPayment_Method] FOREIGN KEY ([PaymentMethodId])
REFERENCES [dbo].[PaymentMethod] ([PaymentMethodId]);
ALTER TABLE [dbo].[SalesPayment] CHECK CONSTRAINT [FK_SalesPayment_Method];
GO
ALTER TABLE [dbo].[SalesReturn] WITH CHECK ADD CONSTRAINT [FK_SalesReturn_Customer] FOREIGN KEY ([CustomerId])
REFERENCES [dbo].[Customer] ([CustomerId]);
ALTER TABLE [dbo].[SalesReturn] CHECK CONSTRAINT [FK_SalesReturn_Customer];
GO
ALTER TABLE [dbo].[SalesReturn] WITH CHECK ADD CONSTRAINT [FK_SalesReturn_Invoice] FOREIGN KEY ([SalesInvoiceId])
REFERENCES [dbo].[SalesInvoice] ([SalesInvoiceId]);
ALTER TABLE [dbo].[SalesReturn] CHECK CONSTRAINT [FK_SalesReturn_Invoice];
GO
ALTER TABLE [dbo].[SalesReturnItem] WITH CHECK ADD CONSTRAINT [FK_SalesReturnItem_InvoiceItem] FOREIGN KEY ([SalesInvoiceItemId])
REFERENCES [dbo].[SalesInvoiceItem] ([SalesInvoiceItemId]);
ALTER TABLE [dbo].[SalesReturnItem] CHECK CONSTRAINT [FK_SalesReturnItem_InvoiceItem];
GO
ALTER TABLE [dbo].[SalesReturnItem] WITH CHECK ADD CONSTRAINT [FK_SalesReturnItem_Return] FOREIGN KEY ([SalesReturnId])
REFERENCES [dbo].[SalesReturn] ([SalesReturnId]) ON DELETE CASCADE;
ALTER TABLE [dbo].[SalesReturnItem] CHECK CONSTRAINT [FK_SalesReturnItem_Return];
GO
ALTER TABLE [dbo].[SalesReturnPayment] WITH CHECK ADD CONSTRAINT [FK_SalesReturnPayment_Method] FOREIGN KEY ([PaymentMethodId])
REFERENCES [dbo].[PaymentMethod] ([PaymentMethodId]);
ALTER TABLE [dbo].[SalesReturnPayment] CHECK CONSTRAINT [FK_SalesReturnPayment_Method];
GO
ALTER TABLE [dbo].[SalesReturnPayment] WITH CHECK ADD CONSTRAINT [FK_SalesReturnPayment_Return] FOREIGN KEY ([SalesReturnId])
REFERENCES [dbo].[SalesReturn] ([SalesReturnId]);
ALTER TABLE [dbo].[SalesReturnPayment] CHECK CONSTRAINT [FK_SalesReturnPayment_Return];
GO
ALTER TABLE [dbo].[SalesReturnSerial] WITH CHECK ADD CONSTRAINT [FK_SalesReturnSerial_Item] FOREIGN KEY ([SalesReturnItemId])
REFERENCES [dbo].[SalesReturnItem] ([SalesReturnItemId]) ON DELETE CASCADE;
ALTER TABLE [dbo].[SalesReturnSerial] CHECK CONSTRAINT [FK_SalesReturnSerial_Item];
GO
ALTER TABLE [dbo].[SalesReturnSerial] WITH CHECK ADD CONSTRAINT [FK_SalesReturnSerial_Unit] FOREIGN KEY ([DeviceUnitId])
REFERENCES [dbo].[DeviceUnit] ([DeviceUnitId]);
ALTER TABLE [dbo].[SalesReturnSerial] CHECK CONSTRAINT [FK_SalesReturnSerial_Unit];
GO
ALTER TABLE [dbo].[StockBalance] WITH CHECK ADD CONSTRAINT [FK_StockBalance_Warehouse] FOREIGN KEY ([WarehouseId])
REFERENCES [dbo].[Warehouse] ([WarehouseId]);
ALTER TABLE [dbo].[StockBalance] CHECK CONSTRAINT [FK_StockBalance_Warehouse];
GO
ALTER TABLE [dbo].[StockMovement] WITH CHECK ADD CONSTRAINT [FK_StockMovement_Warehouse] FOREIGN KEY ([WarehouseId])
REFERENCES [dbo].[Warehouse] ([WarehouseId]);
ALTER TABLE [dbo].[StockMovement] CHECK CONSTRAINT [FK_StockMovement_Warehouse];
GO
ALTER TABLE [dbo].[StockMovementItem] WITH CHECK ADD CONSTRAINT [FK_StockMovementItem_Movement] FOREIGN KEY ([StockMovementId])
REFERENCES [dbo].[StockMovement] ([StockMovementId]) ON DELETE CASCADE;
ALTER TABLE [dbo].[StockMovementItem] CHECK CONSTRAINT [FK_StockMovementItem_Movement];
GO
ALTER TABLE [dbo].[StockMovementSerial] WITH CHECK ADD CONSTRAINT [FK_StockMovementSerial_Item] FOREIGN KEY ([StockMovementItemId])
REFERENCES [dbo].[StockMovementItem] ([StockMovementItemId]) ON DELETE CASCADE;
ALTER TABLE [dbo].[StockMovementSerial] CHECK CONSTRAINT [FK_StockMovementSerial_Item];
GO
ALTER TABLE [dbo].[StockMovementSerial] WITH CHECK ADD CONSTRAINT [FK_StockMovementSerial_Unit] FOREIGN KEY ([DeviceUnitId])
REFERENCES [dbo].[DeviceUnit] ([DeviceUnitId]);
ALTER TABLE [dbo].[StockMovementSerial] CHECK CONSTRAINT [FK_StockMovementSerial_Unit];
GO
ALTER TABLE [dbo].[UserRole] WITH CHECK ADD CONSTRAINT [FK_UserRole_Role] FOREIGN KEY ([RoleId])
REFERENCES [dbo].[Role] ([RoleId]);
ALTER TABLE [dbo].[UserRole] CHECK CONSTRAINT [FK_UserRole_Role];
GO
ALTER TABLE [dbo].[Warehouse] WITH CHECK ADD CONSTRAINT [FK_Warehouse_Branch] FOREIGN KEY ([BranchId])
REFERENCES [dbo].[Branch] ([BranchId]);
ALTER TABLE [dbo].[Warehouse] CHECK CONSTRAINT [FK_Warehouse_Branch];
GO
ALTER TABLE [dbo].[WarrantyClaim] WITH CHECK ADD CONSTRAINT [FK_WarrantyClaim_Customer] FOREIGN KEY ([CustomerId])
REFERENCES [dbo].[Customer] ([CustomerId]);
ALTER TABLE [dbo].[WarrantyClaim] CHECK CONSTRAINT [FK_WarrantyClaim_Customer];
GO
ALTER TABLE [dbo].[WarrantyClaim] WITH CHECK ADD CONSTRAINT [FK_WarrantyClaim_InvoiceItem] FOREIGN KEY ([SalesInvoiceItemId])
REFERENCES [dbo].[SalesInvoiceItem] ([SalesInvoiceItemId]);
ALTER TABLE [dbo].[WarrantyClaim] CHECK CONSTRAINT [FK_WarrantyClaim_InvoiceItem];
GO
ALTER TABLE [dbo].[WarrantyClaim] WITH CHECK ADD CONSTRAINT [FK_WarrantyClaim_Unit] FOREIGN KEY ([DeviceUnitId])
REFERENCES [dbo].[DeviceUnit] ([DeviceUnitId]);
ALTER TABLE [dbo].[WarrantyClaim] CHECK CONSTRAINT [FK_WarrantyClaim_Unit];
GO

-- ============================================================================
-- 5. CREATE VIEWS, FUNCTIONS, STORED PROCEDURES, AND TRIGGERS
-- ============================================================================

-- ----------------------------------------------------------------------------
-- SQL_STORED_PROCEDURE: [dbo].[AddBrand]
-- ----------------------------------------------------------------------------
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER OFF;
GO

-- 8. AddBrand
CREATE   PROCEDURE dbo.AddBrand
    @BrandName VARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    SET @BrandName = LTRIM(RTRIM(@BrandName));

    IF @BrandName IS NULL OR @BrandName = ''
    BEGIN
        RAISERROR('Brand name is required.', 16, 1);
        RETURN;
    END

    IF EXISTS (SELECT 1 FROM dbo.Brand WHERE BrandName = @BrandName AND IsDelete = 0)
    BEGIN
        RAISERROR('A brand with this name already exists.', 16, 1);
        RETURN;
    END

    IF EXISTS (SELECT 1 FROM dbo.Brand WHERE BrandName = @BrandName AND IsDelete = 1)
    BEGIN
        UPDATE dbo.Brand
        SET IsDelete = 0, IsActive = 1, CreatedDate = GETDATE()
        WHERE BrandName = @BrandName;
    END
    ELSE
    BEGIN
        INSERT INTO dbo.Brand (BrandName, IsActive, IsDelete, CreatedDate)
        VALUES (@BrandName, 1, 0, GETDATE());
    END
END;
GO

-- ----------------------------------------------------------------------------
-- SQL_STORED_PROCEDURE: [dbo].[AddCategory]
-- ----------------------------------------------------------------------------
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER OFF;
GO

-- 3. AddCategory
CREATE   PROCEDURE dbo.AddCategory
    @CategoryName VARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    SET @CategoryName = LTRIM(RTRIM(@CategoryName));

    IF @CategoryName IS NULL OR @CategoryName = ''
    BEGIN
        RAISERROR('Category name is required.', 16, 1);
        RETURN;
    END

    IF EXISTS (SELECT 1 FROM dbo.Category WHERE CategoryName = @CategoryName AND IsDelete = 0)
    BEGIN
        RAISERROR('A category with this name already exists.', 16, 1);
        RETURN;
    END

    IF EXISTS (SELECT 1 FROM dbo.Category WHERE CategoryName = @CategoryName AND IsDelete = 1)
    BEGIN
        UPDATE dbo.Category
        SET IsDelete = 0, IsActive = 1, CreatedDate = GETDATE()
        WHERE CategoryName = @CategoryName;
    END
    ELSE
    BEGIN
        INSERT INTO dbo.Category (CategoryName, IsActive, IsDelete, CreatedDate)
        VALUES (@CategoryName, 1, 0, GETDATE());
    END
END;
GO

-- ----------------------------------------------------------------------------
-- SQL_STORED_PROCEDURE: [dbo].[AddProduct]
-- ----------------------------------------------------------------------------
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
CREATE PROCEDURE dbo.AddProduct	
(
	@Name  AS VARCHAR(256),
	@Price AS DECIMAL(18,4),
	@Size AS VARCHAR(64)
)
AS 
BEGIN
	INSERT INTO dbo.Product
	(	
		Name,
		Price, 
		Size, 
		CreatedBY,
		UserId
	)
	VALUES
	(
		@Name, -- Name - varchar(500)
		@Price, -- Price - decimal(18, 2)
		@Size, -- Size - varchar(100)
		1,
		1
	);
END
GO

-- ----------------------------------------------------------------------------
-- SQL_STORED_PROCEDURE: [dbo].[DeleteBrand]
-- ----------------------------------------------------------------------------
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER OFF;
GO

-- 10. DeleteBrand
CREATE   PROCEDURE dbo.DeleteBrand
    @BrandId INT
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.Brand WHERE BrandId = @BrandId AND IsDelete = 0)
    BEGIN
        RAISERROR('Brand not found or already deleted.', 16, 1);
        RETURN;
    END

    IF EXISTS (SELECT 1 FROM dbo.Product WHERE BrandId = @BrandId AND IsDelete = 0)
    BEGIN
        RAISERROR('Cannot delete this brand because it is currently assigned to one or more active products.', 16, 1);
        RETURN;
    END

    UPDATE dbo.Brand
    SET IsDelete = 1, IsActive = 0
    WHERE BrandId = @BrandId;
END;
GO

-- ----------------------------------------------------------------------------
-- SQL_STORED_PROCEDURE: [dbo].[DeleteCategory]
-- ----------------------------------------------------------------------------
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER OFF;
GO

-- 5. DeleteCategory
CREATE   PROCEDURE dbo.DeleteCategory
    @CategoryId INT
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.Category WHERE CategoryId = @CategoryId AND IsDelete = 0)
    BEGIN
        RAISERROR('Category not found or already deleted.', 16, 1);
        RETURN;
    END

    IF EXISTS (SELECT 1 FROM dbo.Product WHERE CategoryId = @CategoryId AND IsDelete = 0)
    BEGIN
        RAISERROR('Cannot delete this category because it is currently assigned to one or more active products.', 16, 1);
        RETURN;
    END

    UPDATE dbo.Category
    SET IsDelete = 1, IsActive = 0
    WHERE CategoryId = @CategoryId;
END;
GO

-- ----------------------------------------------------------------------------
-- SQL_STORED_PROCEDURE: [dbo].[DeleteProduct]
-- ----------------------------------------------------------------------------
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER OFF;
GO

-- 3. Create or Alter DeleteProduct (Soft Delete)
CREATE   PROCEDURE dbo.DeleteProduct
    @ProductId INT,
    @ModifiedBy INT = 1
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.Product WHERE ProductId = @ProductId AND IsDelete = 0)
    BEGIN
        RAISERROR('Product not found or already deleted.', 16, 1);
        RETURN;
    END

    UPDATE dbo.Product
    SET IsDelete = 1,
        IsActive = 0,
        ModifiedBy = ISNULL(@ModifiedBy, 1),
        ModifiedDate = GETDATE()
    WHERE ProductId = @ProductId;
END;
GO

-- ----------------------------------------------------------------------------
-- SQL_STORED_PROCEDURE: [dbo].[GetBrandById]
-- ----------------------------------------------------------------------------
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER OFF;
GO

-- 7. GetBrandById
CREATE   PROCEDURE dbo.GetBrandById
    @BrandId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT BrandId, BrandName, IsActive, CreatedDate
    FROM dbo.Brand
    WHERE BrandId = @BrandId AND IsDelete = 0;
END;
GO

-- ----------------------------------------------------------------------------
-- SQL_STORED_PROCEDURE: [dbo].[GetBrands]
-- ----------------------------------------------------------------------------
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER OFF;
GO

-- ==========================================================
-- BRAND STORED PROCEDURES
-- ==========================================================

-- 6. GetBrands
CREATE   PROCEDURE dbo.GetBrands
AS
BEGIN
    SET NOCOUNT ON;
    SELECT BrandId, BrandName, IsActive, CreatedDate
    FROM dbo.Brand
    WHERE IsDelete = 0
    ORDER BY BrandName ASC;
END;
GO

-- ----------------------------------------------------------------------------
-- SQL_STORED_PROCEDURE: [dbo].[GetCategories]
-- ----------------------------------------------------------------------------
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER OFF;
GO
-- ==========================================================
-- CATEGORY STORED PROCEDURES
-- ==========================================================

-- 1. GetCategories
CREATE   PROCEDURE dbo.GetCategories
AS
BEGIN
    SET NOCOUNT ON;
    SELECT CategoryId, CategoryName, IsActive, CreatedDate
    FROM dbo.Category
    WHERE IsDelete = 0
    ORDER BY CategoryName ASC;
END;
GO

-- ----------------------------------------------------------------------------
-- SQL_STORED_PROCEDURE: [dbo].[GetCategoryById]
-- ----------------------------------------------------------------------------
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER OFF;
GO

-- 2. GetCategoryById
CREATE   PROCEDURE dbo.GetCategoryById
    @CategoryId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT CategoryId, CategoryName, IsActive, CreatedDate
    FROM dbo.Category
    WHERE CategoryId = @CategoryId AND IsDelete = 0;
END;
GO

-- ----------------------------------------------------------------------------
-- SQL_STORED_PROCEDURE: [dbo].[GetProduct]
-- ----------------------------------------------------------------------------
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER OFF;
GO

-- 7. Update GetProduct Stored Procedure
CREATE   PROCEDURE dbo.GetProduct
    @PageNumber INT,
    @PageSize INT,
    @SearchText VARCHAR(100) = NULL,
    @Category VARCHAR(100) = NULL,
    @Brand VARCHAR(100) = NULL,
    @Price DECIMAL(18,2) = NULL,
    @OrderBy VARCHAR(100) = NULL,
    @Order VARCHAR(10) = NULL,
    @TotalRecords INT OUTPUT, 
    @Message VARCHAR(100) OUTPUT,
    @Code INT OUTPUT  
AS
BEGIN
    SET NOCOUNT ON;

    DROP TABLE IF EXISTS #TempProduct;

    SET @OrderBy = NULLIF(LTRIM(RTRIM(@OrderBy)), '');
    SET @Order = UPPER(NULLIF(LTRIM(RTRIM(@Order)), ''));

    SELECT 
        p.ProductId,
        p.[Name],
        b.BrandName AS Brand,
        p.Price,
        c.CategoryName AS Category,
        p.Size,
        p.CreatedDate,
        p.CategoryId,
        p.BrandId
    INTO #TempProduct
    FROM dbo.Product p
    LEFT JOIN dbo.Category c ON p.CategoryId = c.CategoryId
    LEFT JOIN dbo.Brand b ON p.BrandId = b.BrandId
    WHERE p.IsActive = 1  
      AND p.IsDelete = 0 
      AND (@Price IS NULL OR p.Price <= @Price)
      AND (@Brand IS NULL OR b.BrandName = @Brand OR CAST(p.BrandId AS VARCHAR) = @Brand)
      AND (@Category IS NULL OR c.CategoryName = @Category OR CAST(p.CategoryId AS VARCHAR) = @Category)
      AND (
            @SearchText IS NULL 
            OR p.[Name] LIKE '%' + @SearchText + '%'
            OR b.BrandName LIKE '%' + @SearchText + '%'
            OR c.CategoryName LIKE '%' + @SearchText + '%'
      );

    SELECT @TotalRecords = COUNT(1) FROM #TempProduct;

    SELECT	
        ProductId,
        [Name],
        Brand,
        Price,
        Category,
        Size,
        CreatedDate,
        CategoryId,
        BrandId
    FROM #TempProduct
    ORDER BY ProductId DESC
    OFFSET (@PageNumber - 1) * @PageSize ROWS
    FETCH NEXT @PageSize ROWS ONLY;

    IF @TotalRecords > 0
    BEGIN
        SET @Message = 'Success';
        SET @Code = 0;
    END
    ELSE
    BEGIN
        SET @Message = 'No records found';
        SET @Code = 1;
    END

    DROP TABLE IF EXISTS #TempProduct;
END;
GO

-- ----------------------------------------------------------------------------
-- SQL_STORED_PROCEDURE: [dbo].[GetProduct1]
-- ----------------------------------------------------------------------------
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
CREATE PROCEDURE dbo.GetProduct1
(
	@PageNo INT,
	@PageSize INT,
    @SearchText VARCHAR(100) = NULL
)
AS
BEGIN
	SELECT 
		ProductId, 
		Name, 
		Price, 
		Size, 
		CreatedDate, 
		CategoryId, 
		BrandId, 
		UserId 
	FROM 
		dbo.Product
	WHERE	
		IsActive = 1 AND
        (
			@SearchText IS NULL 
			OR [Name] LIKE '%' + @SearchText + '%'
		)
	ORDER BY ProductId
	OFFSET (@PageNo - 1 ) * @PageSize ROW
	FETCH NEXT @PageSize ROWS ONLY;
END
GO

-- ----------------------------------------------------------------------------
-- SQL_STORED_PROCEDURE: [dbo].[GetProductById]
-- ----------------------------------------------------------------------------
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER OFF;
GO
-- 1. Create or Alter GetProductById
CREATE   PROCEDURE dbo.GetProductById
    @ProductId INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        p.ProductId,
        p.[Name],
        p.BrandId,
        b.BrandName AS Brand,
        p.CategoryId,
        c.CategoryName AS Category,
        p.Price,
        p.Size,
        p.CreatedDate
    FROM dbo.Product p
    LEFT JOIN dbo.Category c ON p.CategoryId = c.CategoryId
    LEFT JOIN dbo.Brand b ON p.BrandId = b.BrandId
    WHERE p.ProductId = @ProductId
      AND p.IsDelete = 0;
END;
GO

-- ----------------------------------------------------------------------------
-- SQL_STORED_PROCEDURE: [dbo].[UpdateBrand]
-- ----------------------------------------------------------------------------
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER OFF;
GO

-- 9. UpdateBrand
CREATE   PROCEDURE dbo.UpdateBrand
    @BrandId INT,
    @BrandName VARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    SET @BrandName = LTRIM(RTRIM(@BrandName));

    IF NOT EXISTS (SELECT 1 FROM dbo.Brand WHERE BrandId = @BrandId AND IsDelete = 0)
    BEGIN
        RAISERROR('Brand not found.', 16, 1);
        RETURN;
    END

    IF @BrandName IS NULL OR @BrandName = ''
    BEGIN
        RAISERROR('Brand name is required.', 16, 1);
        RETURN;
    END

    IF EXISTS (SELECT 1 FROM dbo.Brand WHERE BrandName = @BrandName AND BrandId <> @BrandId AND IsDelete = 0)
    BEGIN
        RAISERROR('Another brand with this name already exists.', 16, 1);
        RETURN;
    END

    UPDATE dbo.Brand
    SET BrandName = @BrandName
    WHERE BrandId = @BrandId;
END;
GO

-- ----------------------------------------------------------------------------
-- SQL_STORED_PROCEDURE: [dbo].[UpdateCategory]
-- ----------------------------------------------------------------------------
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER OFF;
GO

-- 4. UpdateCategory
CREATE   PROCEDURE dbo.UpdateCategory
    @CategoryId INT,
    @CategoryName VARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    SET @CategoryName = LTRIM(RTRIM(@CategoryName));

    IF NOT EXISTS (SELECT 1 FROM dbo.Category WHERE CategoryId = @CategoryId AND IsDelete = 0)
    BEGIN
        RAISERROR('Category not found.', 16, 1);
        RETURN;
    END

    IF @CategoryName IS NULL OR @CategoryName = ''
    BEGIN
        RAISERROR('Category name is required.', 16, 1);
        RETURN;
    END

    IF EXISTS (SELECT 1 FROM dbo.Category WHERE CategoryName = @CategoryName AND CategoryId <> @CategoryId AND IsDelete = 0)
    BEGIN
        RAISERROR('Another category with this name already exists.', 16, 1);
        RETURN;
    END

    UPDATE dbo.Category
    SET CategoryName = @CategoryName
    WHERE CategoryId = @CategoryId;
END;
GO

-- ----------------------------------------------------------------------------
-- SQL_STORED_PROCEDURE: [dbo].[UpdateProduct]
-- ----------------------------------------------------------------------------
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER OFF;
GO

-- 2. Create or Alter UpdateProduct
CREATE   PROCEDURE dbo.UpdateProduct
    @ProductId INT,
    @Name VARCHAR(500),
    @BrandId INT,
    @CategoryId INT,
    @Price DECIMAL(18,2),
    @Size VARCHAR(100) = NULL,
    @ModifiedBy INT = 1
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM dbo.Product WHERE ProductId = @ProductId AND IsDelete = 0)
    BEGIN
        RAISERROR('Product not found.', 16, 1);
        RETURN;
    END

    IF @Name IS NULL OR LTRIM(RTRIM(@Name)) = ''
    BEGIN
        RAISERROR('Product Name is required.', 16, 1);
        RETURN;
    END

    IF @BrandId IS NULL OR @BrandId <= 0 OR NOT EXISTS (SELECT 1 FROM dbo.Brand WHERE BrandId = @BrandId AND IsDelete = 0)
    BEGIN
        RAISERROR('Please select a valid Brand.', 16, 1);
        RETURN;
    END

    IF @CategoryId IS NULL OR @CategoryId <= 0 OR NOT EXISTS (SELECT 1 FROM dbo.Category WHERE CategoryId = @CategoryId AND IsDelete = 0)
    BEGIN
        RAISERROR('Please select a valid Category.', 16, 1);
        RETURN;
    END

    IF @Price IS NULL OR @Price < 0
    BEGIN
        RAISERROR('Price must be greater than or equal to 0.', 16, 1);
        RETURN;
    END

    UPDATE dbo.Product
    SET [Name] = LTRIM(RTRIM(@Name)),
        BrandId = @BrandId,
        CategoryId = @CategoryId,
        Price = @Price,
        Size = NULLIF(LTRIM(RTRIM(@Size)), ''),
        ModifiedBy = ISNULL(@ModifiedBy, 1),
        ModifiedDate = GETDATE()
    WHERE ProductId = @ProductId;
END;
GO

-- ============================================================================
-- RESTORE COMPLETE
-- ============================================================================
PRINT N'Database [Product] restored successfully: 39 tables, 46 foreign keys, 16 programmable objects, 61 total rows inserted.';
GO
SET NOEXEC OFF;
GO
