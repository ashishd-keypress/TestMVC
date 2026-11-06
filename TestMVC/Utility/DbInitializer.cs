using Microsoft.Data.SqlClient;
using System;

namespace Utility
{
    public static class DbInitializer
    {
        private static bool _initialized = false;
        private static readonly object _lock = new object();

        public static void EnsureDatabaseSetup(string connectionString)
        {
            if (_initialized) return;

            lock (_lock)
            {
                if (_initialized) return;

                try
                {
                    using var connection = new SqlConnection(connectionString);
                    connection.Open();

                    // 1. Ensure Users table exists (WITHOUT IsApproved)
                    using (var cmd = new SqlCommand(@"
                        IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Users')
                        BEGIN
                            CREATE TABLE dbo.Users (
                                UserId INT IDENTITY(1,1) PRIMARY KEY,
                                Username NVARCHAR(100) NOT NULL UNIQUE,
                                PasswordHash NVARCHAR(500) NOT NULL,
                                PasswordSalt NVARCHAR(200) NOT NULL,
                                MobileNumber NVARCHAR(20) NOT NULL,
                                IsDelete BIT NOT NULL DEFAULT 0,
                                CreatedDate DATETIME NOT NULL DEFAULT GETDATE(),
                                LastLoginDate DATETIME NULL
                            );
                        END;", connection))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    // 2. Drop IsApproved column and its default constraint if it exists (completely remove User Approvals from DB)
                    using (var cmd = new SqlCommand(@"
                        IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Users') AND name = 'IsApproved')
                        BEGIN
                            DECLARE @constraintName NVARCHAR(200);
                            SELECT @constraintName = d.name
                            FROM sys.default_constraints d
                            JOIN sys.columns c ON d.parent_column_id = c.column_id AND d.parent_object_id = c.object_id
                            WHERE d.parent_object_id = OBJECT_ID('dbo.Users') AND c.name = 'IsApproved';

                            IF @constraintName IS NOT NULL
                            BEGIN
                                EXEC('ALTER TABLE dbo.Users DROP CONSTRAINT ' + @constraintName);
                            END

                            ALTER TABLE dbo.Users DROP COLUMN IsApproved;
                        END;", connection))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    // 3. Seed / Ensure Admin User: Ashish@123 with Password: xyz@123
                    PasswordSecurity.CreatePasswordHash("xyz@123", out string hash, out string salt);

                    using (var cmd = new SqlCommand(@"
                        IF EXISTS (SELECT 1 FROM dbo.Users WHERE LOWER(Username) = 'ashish@123')
                        BEGIN
                            UPDATE dbo.Users 
                            SET PasswordHash = @PasswordHash, 
                                PasswordSalt = @PasswordSalt,
                                IsDelete = 0
                            WHERE LOWER(Username) = 'ashish@123';
                        END
                        ELSE IF EXISTS (SELECT 1 FROM dbo.Users WHERE UserId = 1)
                        BEGIN
                            -- Update user 1 to Ashish@123 so that existing brands/products/categories (owned by UserId 1) are retained
                            UPDATE dbo.Users
                            SET Username = 'Ashish@123',
                                PasswordHash = @PasswordHash,
                                PasswordSalt = @PasswordSalt,
                                IsDelete = 0
                            WHERE UserId = 1;
                        END
                        ELSE
                        BEGIN
                            INSERT INTO dbo.Users (Username, PasswordHash, PasswordSalt, MobileNumber, IsDelete, CreatedDate)
                            VALUES ('Ashish@123', @PasswordHash, @PasswordSalt, '9876543210', 0, GETDATE());
                        END;", connection))
                    {
                        cmd.Parameters.AddWithValue("@PasswordHash", hash);
                        cmd.Parameters.AddWithValue("@PasswordSalt", salt);
                        cmd.ExecuteNonQuery();
                    }

                    // 4. Ensure Brand.UserId
                    using (var cmd = new SqlCommand(@"
                        IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Brand')
                           AND NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Brand') AND name = 'UserId')
                        BEGIN
                            ALTER TABLE dbo.Brand ADD UserId INT NULL;
                        END;", connection))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    // Backfill Brand.UserId
                    using (var cmd = new SqlCommand(@"
                        IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Brand') AND name = 'UserId')
                        BEGIN
                            EXEC('UPDATE dbo.Brand SET UserId = 1 WHERE UserId IS NULL;');
                        END;", connection))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    // 5. Ensure Category.UserId
                    using (var cmd = new SqlCommand(@"
                        IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Category')
                           AND NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Category') AND name = 'UserId')
                        BEGIN
                            ALTER TABLE dbo.Category ADD UserId INT NULL;
                        END;", connection))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    // Backfill Category.UserId
                    using (var cmd = new SqlCommand(@"
                        IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Category') AND name = 'UserId')
                        BEGIN
                            EXEC('UPDATE dbo.Category SET UserId = 1 WHERE UserId IS NULL;');
                        END;", connection))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    // 6. Ensure Product.UserId
                    using (var cmd = new SqlCommand(@"
                        IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Product')
                           AND NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Product') AND name = 'UserId')
                        BEGIN
                            ALTER TABLE dbo.Product ADD UserId INT NULL;
                        END;", connection))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    // Backfill Product.UserId
                    using (var cmd = new SqlCommand(@"
                        IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Product') AND name = 'UserId')
                        BEGIN
                            EXEC('UPDATE dbo.Product SET UserId = ISNULL(CreatedBY, 1) WHERE UserId IS NULL;');
                        END;", connection))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    // 7. Ensure Product.Size allows NULL (Size is not required)
                    using (var cmd = new SqlCommand(@"
                        IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Product' AND COLUMN_NAME = 'Size' AND IS_NULLABLE = 'NO')
                        BEGIN
                            ALTER TABLE dbo.Product ALTER COLUMN Size VARCHAR(500) NULL;
                        END;", connection))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    // 8. Ensure Image and ImageUrl columns exist on Brand, Category, Product tables
                    using (var cmd = new SqlCommand(@"
                        IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Brand')
                        BEGIN
                            IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Brand') AND name = 'Image')
                                ALTER TABLE dbo.Brand ADD [Image] NVARCHAR(500) NULL;
                            IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Brand') AND name = 'ImageUrl')
                                ALTER TABLE dbo.Brand ADD ImageUrl NVARCHAR(500) NULL;
                        END;

                        IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Category')
                        BEGIN
                            IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Category') AND name = 'Image')
                                ALTER TABLE dbo.Category ADD [Image] NVARCHAR(500) NULL;
                            IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Category') AND name = 'ImageUrl')
                                ALTER TABLE dbo.Category ADD ImageUrl NVARCHAR(500) NULL;
                        END;

                        IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Product')
                        BEGIN
                            IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Product') AND name = 'Image')
                                ALTER TABLE dbo.Product ADD [Image] NVARCHAR(500) NULL;
                            IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Product') AND name = 'ImageUrl')
                                ALTER TABLE dbo.Product ADD ImageUrl NVARCHAR(500) NULL;
                        END;

                        IF EXISTS (SELECT * FROM sys.tables WHERE name = 'DeviceUnit')
                        BEGIN
                            IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.DeviceUnit') AND name = 'CostPrice')
                                ALTER TABLE dbo.DeviceUnit ADD CostPrice DECIMAL(18,2) NULL;
                            IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.DeviceUnit') AND name = 'Notes')
                                ALTER TABLE dbo.DeviceUnit ADD Notes NVARCHAR(500) NULL;
                        END;

                        IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Expense')
                        BEGIN
                            IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Expense') AND name = 'CreatedDate')
                                ALTER TABLE dbo.Expense ADD CreatedDate DATETIME2 NOT NULL CONSTRAINT DF_Expense_CreatedDate DEFAULT SYSUTCDATETIME();
                        END;", connection))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    _initialized = true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Database initialization error: {ex.Message}");
                }
            }
        }
    }
}
