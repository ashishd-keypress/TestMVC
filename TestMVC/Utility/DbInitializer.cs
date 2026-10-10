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

                    // 1. Ensure Users table
                    using (var cmd = new SqlCommand(@"
                        IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Users')
                        BEGIN
                            CREATE TABLE dbo.Users (
                                UserId INT IDENTITY(1,1) PRIMARY KEY,
                                Username NVARCHAR(100) NOT NULL UNIQUE,
                                PasswordHash NVARCHAR(500) NOT NULL,
                                PasswordSalt NVARCHAR(200) NOT NULL,
                                MobileNumber NVARCHAR(20) NOT NULL,
                                IsApproved BIT NOT NULL DEFAULT 0,
                                IsDelete BIT NOT NULL DEFAULT 0,
                                CreatedDate DATETIME NOT NULL DEFAULT GETDATE(),
                                LastLoginDate DATETIME NULL
                            );
                        END;", connection))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    // 2. Ensure Brand.UserId
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

                    // 3. Ensure Category.UserId
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

                    // 4. Ensure Product.UserId
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
