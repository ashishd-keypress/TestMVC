-- ==========================================================
-- 1. Create Category Table
-- ==========================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Category')
BEGIN
    CREATE TABLE dbo.Category (
        CategoryId INT IDENTITY(1,1) PRIMARY KEY,
        CategoryName VARCHAR(100) NOT NULL UNIQUE,
        IsActive BIT NOT NULL DEFAULT 1,
        IsDelete BIT NOT NULL DEFAULT 0,
        CreatedDate DATETIME NOT NULL DEFAULT GETDATE()
    );
END
GO

-- ==========================================================
-- 2. Create Brand Table
-- ==========================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Brand')
BEGIN
    CREATE TABLE dbo.Brand (
        BrandId INT IDENTITY(1,1) PRIMARY KEY,
        BrandName VARCHAR(100) NOT NULL UNIQUE,
        IsActive BIT NOT NULL DEFAULT 1,
        IsDelete BIT NOT NULL DEFAULT 0,
        CreatedDate DATETIME NOT NULL DEFAULT GETDATE()
    );
END
GO

-- ==========================================================
-- 3. Populate Category and Brand from existing Product data
-- ==========================================================
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Product') AND name = 'Category')
BEGIN
    INSERT INTO dbo.Category (CategoryName)
    SELECT DISTINCT LTRIM(RTRIM(Category))
    FROM dbo.Product
    WHERE Category IS NOT NULL AND LTRIM(RTRIM(Category)) <> ''
      AND LTRIM(RTRIM(Category)) NOT IN (SELECT CategoryName FROM dbo.Category);
END
GO

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Product') AND name = 'Brand')
BEGIN
    INSERT INTO dbo.Brand (BrandName)
    SELECT DISTINCT LTRIM(RTRIM(Brand))
    FROM dbo.Product
    WHERE Brand IS NOT NULL AND LTRIM(RTRIM(Brand)) <> ''
      AND LTRIM(RTRIM(Brand)) NOT IN (SELECT BrandName FROM dbo.Brand);
END
GO

-- ==========================================================
-- 4. Add CategoryId, BrandId, IsActive, IsDelete to dbo.Product
-- ==========================================================
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Product') AND name = 'CategoryId')
BEGIN
    ALTER TABLE dbo.Product ADD CategoryId INT NULL CONSTRAINT FK_Product_Category FOREIGN KEY REFERENCES dbo.Category(CategoryId);
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Product') AND name = 'BrandId')
BEGIN
    ALTER TABLE dbo.Product ADD BrandId INT NULL CONSTRAINT FK_Product_Brand FOREIGN KEY REFERENCES dbo.Brand(BrandId);
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Product') AND name = 'IsActive')
BEGIN
    ALTER TABLE dbo.Product ADD IsActive BIT NOT NULL CONSTRAINT DF_Product_IsActive DEFAULT 1;
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Product') AND name = 'IsDelete')
BEGIN
    ALTER TABLE dbo.Product ADD IsDelete BIT NOT NULL CONSTRAINT DF_Product_IsDelete DEFAULT 0;
END
GO

-- Ensure all NULL IsDelete/IsActive are sanitized to active non-deleted defaults
UPDATE dbo.Product SET IsDelete = 0 WHERE IsDelete IS NULL;
UPDATE dbo.Product SET IsActive = 1 WHERE IsActive IS NULL;
UPDATE dbo.Category SET IsDelete = 0 WHERE IsDelete IS NULL;
UPDATE dbo.Category SET IsActive = 1 WHERE IsActive IS NULL;
UPDATE dbo.Brand SET IsDelete = 0 WHERE IsDelete IS NULL;
UPDATE dbo.Brand SET IsActive = 1 WHERE IsActive IS NULL;
GO

-- ==========================================================
-- 5. Update CategoryId and BrandId values from existing strings
-- ==========================================================
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Product') AND name = 'Category')
BEGIN
    UPDATE p
    SET p.CategoryId = c.CategoryId
    FROM dbo.Product p
    JOIN dbo.Category c ON LTRIM(RTRIM(p.Category)) = c.CategoryName
    WHERE p.CategoryId IS NULL;
END
GO

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Product') AND name = 'Brand')
BEGIN
    UPDATE p
    SET p.BrandId = b.BrandId
    FROM dbo.Product p
    JOIN dbo.Brand b ON LTRIM(RTRIM(p.Brand)) = b.BrandName
    WHERE p.BrandId IS NULL;
END
GO

-- ==========================================================
-- 6. Drop old columns Category, Brand, DiscountedPrice
-- ==========================================================
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Product') AND name = 'Category')
BEGIN
    ALTER TABLE dbo.Product DROP COLUMN Category;
END
GO

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Product') AND name = 'Brand')
BEGIN
    ALTER TABLE dbo.Product DROP COLUMN Brand;
END
GO

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Product') AND name = 'DiscountedPrice')
BEGIN
    ALTER TABLE dbo.Product DROP COLUMN DiscountedPrice;
END
GO

-- ==========================================================
-- 7. Stored Procedure: GetProduct (Paging & Search)
-- ==========================================================
CREATE OR ALTER PROCEDURE dbo.GetProduct
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
        p.BrandId,
        ISNULL(p.IsActive, 1) AS IsActive
    INTO #TempProduct
    FROM dbo.Product p
    LEFT JOIN dbo.Category c ON p.CategoryId = c.CategoryId
    LEFT JOIN dbo.Brand b ON p.BrandId = b.BrandId
    WHERE ISNULL(p.IsDelete, 0) = 0 
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
        BrandId,
        IsActive
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

-- ==========================================================
-- 8. Stored Procedure: AddProduct
-- ==========================================================
CREATE OR ALTER PROCEDURE dbo.AddProduct
    @Name VARCHAR(500),
    @BrandId INT,
    @CategoryId INT,
    @Price DECIMAL(18,2),
    @Size VARCHAR(100) = NULL,
    @IsActive BIT = 1,
    @CreatedBY INT = 1
AS
BEGIN
    SET NOCOUNT ON;

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

    INSERT INTO dbo.Product (
        [Name],
        BrandId,
        CategoryId,
        Price,
        Size,
        IsActive,
        IsDelete,
        CreatedBY,
        CreatedDate
    )
    VALUES (
        LTRIM(RTRIM(@Name)),
        @BrandId,
        @CategoryId,
        @Price,
        NULLIF(LTRIM(RTRIM(@Size)), ''),
        ISNULL(@IsActive, 1),
        0,
        ISNULL(@CreatedBY, 1),
        GETDATE()
    );
END;
GO

-- ==========================================================
-- 9. Stored Procedure: GetProductById
-- ==========================================================
CREATE OR ALTER PROCEDURE dbo.GetProductById
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
        p.IsActive,
        p.CreatedDate
    FROM dbo.Product p
    LEFT JOIN dbo.Category c ON p.CategoryId = c.CategoryId
    LEFT JOIN dbo.Brand b ON p.BrandId = b.BrandId
    WHERE p.ProductId = @ProductId
      AND p.IsDelete = 0;
END;
GO

-- ==========================================================
-- 10. Stored Procedure: UpdateProduct
-- ==========================================================
CREATE OR ALTER PROCEDURE dbo.UpdateProduct
    @ProductId INT,
    @Name VARCHAR(500),
    @BrandId INT,
    @CategoryId INT,
    @Price DECIMAL(18,2),
    @Size VARCHAR(100) = NULL,
    @IsActive BIT = 1,
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
        IsActive = ISNULL(@IsActive, IsActive),
        ModifiedBy = ISNULL(@ModifiedBy, 1),
        ModifiedDate = GETDATE()
    WHERE ProductId = @ProductId;
END;
GO

-- ==========================================================
-- 11. Stored Procedure: DeleteProduct (Soft Delete)
-- ==========================================================
CREATE OR ALTER PROCEDURE dbo.DeleteProduct
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

-- ==========================================================
-- 12. Category Stored Procedures (CRUD)
-- ==========================================================
CREATE OR ALTER PROCEDURE dbo.GetCategories
AS
BEGIN
    SET NOCOUNT ON;
    SELECT CategoryId, CategoryName, IsActive, CreatedDate
    FROM dbo.Category
    WHERE IsDelete = 0
    ORDER BY CategoryName ASC;
END;
GO

CREATE OR ALTER PROCEDURE dbo.GetCategoryById
    @CategoryId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT CategoryId, CategoryName, IsActive, CreatedDate
    FROM dbo.Category
    WHERE CategoryId = @CategoryId AND IsDelete = 0;
END;
GO

CREATE OR ALTER PROCEDURE dbo.AddCategory
    @CategoryName VARCHAR(100),
    @IsActive BIT = 1
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
        SET IsDelete = 0, IsActive = ISNULL(@IsActive, 1), CreatedDate = GETDATE()
        WHERE CategoryName = @CategoryName;
    END
    ELSE
    BEGIN
        INSERT INTO dbo.Category (CategoryName, IsActive, IsDelete, CreatedDate)
        VALUES (@CategoryName, ISNULL(@IsActive, 1), 0, GETDATE());
    END
END;
GO

CREATE OR ALTER PROCEDURE dbo.UpdateCategory
    @CategoryId INT,
    @CategoryName VARCHAR(100),
    @IsActive BIT = 1
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
    SET CategoryName = @CategoryName,
        IsActive = ISNULL(@IsActive, IsActive)
    WHERE CategoryId = @CategoryId;
END;
GO

CREATE OR ALTER PROCEDURE dbo.DeleteCategory
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

-- ==========================================================
-- 13. Brand Stored Procedures (CRUD)
-- ==========================================================
CREATE OR ALTER PROCEDURE dbo.GetBrands
AS
BEGIN
    SET NOCOUNT ON;
    SELECT BrandId, BrandName, IsActive, CreatedDate
    FROM dbo.Brand
    WHERE IsDelete = 0
    ORDER BY BrandName ASC;
END;
GO

CREATE OR ALTER PROCEDURE dbo.GetBrandById
    @BrandId INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT BrandId, BrandName, IsActive, CreatedDate
    FROM dbo.Brand
    WHERE BrandId = @BrandId AND IsDelete = 0;
END;
GO

CREATE OR ALTER PROCEDURE dbo.AddBrand
    @BrandName VARCHAR(100),
    @IsActive BIT = 1
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
        SET IsDelete = 0, IsActive = ISNULL(@IsActive, 1), CreatedDate = GETDATE()
        WHERE BrandName = @BrandName;
    END
    ELSE
    BEGIN
        INSERT INTO dbo.Brand (BrandName, IsActive, IsDelete, CreatedDate)
        VALUES (@BrandName, ISNULL(@IsActive, 1), 0, GETDATE());
    END
END;
GO

CREATE OR ALTER PROCEDURE dbo.UpdateBrand
    @BrandId INT,
    @BrandName VARCHAR(100),
    @IsActive BIT = 1
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
    SET BrandName = @BrandName,
        IsActive = ISNULL(@IsActive, IsActive)
    WHERE BrandId = @BrandId;
END;
GO

CREATE OR ALTER PROCEDURE dbo.DeleteBrand
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

-- ==========================================================
-- 10. Users Table for Secure Authentication & Approval
-- ==========================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Users')
BEGIN
    CREATE TABLE dbo.Users (
        UserId INT IDENTITY(1,1) PRIMARY KEY,
        Username NVARCHAR(100) NOT NULL UNIQUE,
        PasswordHash NVARCHAR(500) NOT NULL,
        PasswordSalt NVARCHAR(200) NOT NULL,
        MobileNumber NVARCHAR(20) NOT NULL,
        IsApproved BIT NOT NULL DEFAULT 0, -- False: pending approval, True: allowed to login
        IsDelete BIT NOT NULL DEFAULT 0,
        CreatedDate DATETIME NOT NULL DEFAULT GETDATE(),
        LastLoginDate DATETIME NULL
    );
END
GO

-- ==========================================================
-- 11. Multi-Tenant User Scoping: Add UserId to Brand, Category, Product
-- ==========================================================
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Brand') AND name = 'UserId')
BEGIN
    ALTER TABLE dbo.Brand ADD UserId INT NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Category') AND name = 'UserId')
BEGIN
    ALTER TABLE dbo.Category ADD UserId INT NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Product') AND name = 'UserId')
BEGIN
    ALTER TABLE dbo.Product ADD UserId INT NULL;
END
GO

-- Backfill existing legacy records with UserId = 1 (primary/admin user)
UPDATE dbo.Brand SET UserId = 1 WHERE UserId IS NULL;
UPDATE dbo.Category SET UserId = 1 WHERE UserId IS NULL;
UPDATE dbo.Product SET UserId = ISNULL(CreatedBY, 1) WHERE UserId IS NULL;
GO


