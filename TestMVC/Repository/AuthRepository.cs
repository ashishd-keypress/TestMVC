using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using TestMVC.Interface;
using TestMVC.Models.ReqRes;
using Utility;

namespace TestMVC.Repository
{
    public class AuthRepository : IAuthRepository
    {
        private readonly string _connectionString;
        private static bool _tableEnsured = false;
        private static readonly object _tableLock = new object();

        public AuthRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("DefaultConnection is missing in appsettings.json");

            EnsureTableOnce();
        }

        private void EnsureTableOnce()
        {
            if (!_tableEnsured)
            {
                lock (_tableLock)
                {
                    if (!_tableEnsured)
                    {
                        try
                        {
                            EnsureUserTableCreatedAsync().GetAwaiter().GetResult();
                            _tableEnsured = true;
                        }
                        catch
                        {
                            // Table check fallback
                        }
                    }
                }
            }
        }

        public async Task EnsureUserTableCreatedAsync()
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var cmd = new SqlCommand(@"
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
                END;

                -- Ensure UserId column in Brand
                IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Brand')
                   AND NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Brand') AND name = 'UserId')
                BEGIN
                    ALTER TABLE dbo.Brand ADD UserId INT NULL;
                END;

                -- Ensure UserId column in Category
                IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Category')
                   AND NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Category') AND name = 'UserId')
                BEGIN
                    ALTER TABLE dbo.Category ADD UserId INT NULL;
                END;

                -- Ensure UserId column in Product
                IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Product')
                   AND NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Product') AND name = 'UserId')
                BEGIN
                    ALTER TABLE dbo.Product ADD UserId INT NULL;
                END;

                -- Backfill existing legacy records with NULL UserId to 1 so demo data is accessible to User 1
                IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Brand') AND name = 'UserId')
                BEGIN
                    UPDATE dbo.Brand SET UserId = 1 WHERE UserId IS NULL;
                END;

                IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Category') AND name = 'UserId')
                BEGIN
                    UPDATE dbo.Category SET UserId = 1 WHERE UserId IS NULL;
                END;

                IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Product') AND name = 'UserId')
                BEGIN
                    UPDATE dbo.Product SET UserId = 1 WHERE UserId IS NULL;
                END;", connection);

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<Tuple<RegisterRes, int, string>> RegisterAsync(RegisterReq model, string passwordHash, string passwordSalt)
        {
            string strDBError = string.Empty;
            var res = new RegisterRes
            {
                Username = model.Username,
                MobileNumber = model.MobileNumber,
                IsApproved = false,
                Success = false
            };

            using (var connection = new SqlConnection(_connectionString))
            {
                try
                {
                    await connection.OpenAsync();

                    // Check if username already exists
                    using (var checkCmd = new SqlCommand("SELECT COUNT(1) FROM dbo.Users WHERE LOWER(Username) = LOWER(@Username) AND IsDelete = 0;", connection))
                    {
                        checkCmd.Parameters.AddWithValue("@Username", model.Username.Trim());
                        int existingCount = Convert.ToInt32(await checkCmd.ExecuteScalarAsync() ?? 0);
                        if (existingCount > 0)
                        {
                            res.Success = false;
                            res.Message = "This username is already taken. Please choose another.";
                            return new Tuple<RegisterRes, int, string>(res, 0, strDBError);
                        }
                    }

                    // Check if mobile number already exists
                    using (var checkMobCmd = new SqlCommand("SELECT COUNT(1) FROM dbo.Users WHERE MobileNumber = @MobileNumber AND IsDelete = 0;", connection))
                    {
                        checkMobCmd.Parameters.AddWithValue("@MobileNumber", model.MobileNumber.Trim());
                        int existingMob = Convert.ToInt32(await checkMobCmd.ExecuteScalarAsync() ?? 0);
                        if (existingMob > 0)
                        {
                            res.Success = false;
                            res.Message = "This mobile number is already registered.";
                            return new Tuple<RegisterRes, int, string>(res, 0, strDBError);
                        }
                    }

                    // Insert user with IsApproved = 0 (Requires approval before login)
                    using (var insertCmd = new SqlCommand(@"
                        INSERT INTO dbo.Users (Username, PasswordHash, PasswordSalt, MobileNumber, IsApproved, IsDelete, CreatedDate)
                        VALUES (@Username, @PasswordHash, @PasswordSalt, @MobileNumber, 0, 0, GETDATE());
                        SELECT SCOPE_IDENTITY();", connection))
                    {
                        insertCmd.Parameters.AddWithValue("@Username", model.Username.Trim());
                        insertCmd.Parameters.AddWithValue("@PasswordHash", passwordHash);
                        insertCmd.Parameters.AddWithValue("@PasswordSalt", passwordSalt);
                        insertCmd.Parameters.AddWithValue("@MobileNumber", model.MobileNumber.Trim());

                        var newId = await insertCmd.ExecuteScalarAsync();
                        if (newId != null && Convert.ToInt32(newId) > 0)
                        {
                            res.UserId = Convert.ToInt32(newId);
                            res.Success = true;
                            res.IsApproved = false;
                            res.Message = "Registration successful! Your account is created but pending admin approval. You can log in once approved.";
                        }
                        else
                        {
                            res.Success = false;
                            res.Message = "Could not complete registration. Please try again.";
                        }
                    }
                }
                catch (Exception ex)
                {
                    strDBError = JsonConvert.SerializeObject(ex);
                    res.Success = false;
                    res.Message = "Database error during registration.";
                }
                finally
                {
                    if (connection.State == ConnectionState.Open) await connection.CloseAsync();
                }
            }

            return new Tuple<RegisterRes, int, string>(res, res.Success ? 1 : 0, strDBError);
        }

        public async Task<Tuple<LoginRes, int, string>> ValidateLoginAsync(LoginReq model)
        {
            string strDBError = string.Empty;
            var res = new LoginRes
            {
                Username = model.Username,
                Success = false
            };

            using (var connection = new SqlConnection(_connectionString))
            {
                try
                {
                    await connection.OpenAsync();

                    string? storedHash = null;
                    string? storedSalt = null;
                    int userId = 0;
                    string mobile = string.Empty;
                    bool isApproved = false;

                    using (var cmd = new SqlCommand(@"
                        SELECT UserId, Username, PasswordHash, PasswordSalt, MobileNumber, IsApproved 
                        FROM dbo.Users 
                        WHERE LOWER(Username) = LOWER(@Username) AND IsDelete = 0;", connection))
                    {
                        cmd.Parameters.AddWithValue("@Username", model.Username.Trim());

                        using var reader = await cmd.ExecuteReaderAsync();
                        if (await reader.ReadAsync())
                        {
                            userId = reader.GetInt32(0);
                            res.Username = reader.GetString(1);
                            storedHash = reader.GetString(2);
                            storedSalt = reader.GetString(3);
                            mobile = reader.GetString(4);
                            isApproved = reader.GetBoolean(5);
                        }
                    }

                    if (userId == 0 || string.IsNullOrEmpty(storedHash) || string.IsNullOrEmpty(storedSalt))
                    {
                        res.Success = false;
                        res.Message = "Invalid username or password.";
                        return new Tuple<LoginRes, int, string>(res, 0, strDBError);
                    }

                    // Secure password verification
                    bool isValidPassword = PasswordSecurity.VerifyPasswordHash(model.Password, storedHash, storedSalt);
                    if (!isValidPassword)
                    {
                        res.Success = false;
                        res.Message = "Invalid username or password.";
                        return new Tuple<LoginRes, int, string>(res, 0, strDBError);
                    }

                    res.UserId = userId;
                    res.MobileNumber = mobile;
                    res.IsApproved = isApproved;

                    // Crucial business requirement: Check if user is approved (IsApproved == true)
                    if (!isApproved)
                    {
                        res.Success = false;
                        res.Message = "Your account is pending admin approval (Status = False). You cannot log in yet.";
                        return new Tuple<LoginRes, int, string>(res, 0, strDBError);
                    }

                    // Update last login timestamp
                    using (var updateCmd = new SqlCommand("UPDATE dbo.Users SET LastLoginDate = GETDATE() WHERE UserId = @UserId;", connection))
                    {
                        updateCmd.Parameters.AddWithValue("@UserId", userId);
                        await updateCmd.ExecuteNonQueryAsync();
                    }

                    res.Success = true;
                    res.Message = "Login successful!";
                    res.Token = Guid.NewGuid().ToString("N"); // session token
                }
                catch (Exception ex)
                {
                    strDBError = JsonConvert.SerializeObject(ex);
                    res.Success = false;
                    res.Message = "Database error during login validation.";
                }
                finally
                {
                    if (connection.State == ConnectionState.Open) await connection.CloseAsync();
                }
            }

            return new Tuple<LoginRes, int, string>(res, res.Success ? 1 : 0, strDBError);
        }

        public async Task<Tuple<List<UserItemRes>, int, string>> GetUsersAsync(GetUserReq model)
        {
            string strDBError = string.Empty;
            var list = new List<UserItemRes>();
            int totalRecords = 0;

            using (var connection = new SqlConnection(_connectionString))
            {
                try
                {
                    await connection.OpenAsync();

                    using (var countCmd = new SqlCommand(@"
                        SELECT COUNT(1) 
                        FROM dbo.Users 
                        WHERE IsDelete = 0
                          AND (@IsApproved IS NULL OR IsApproved = @IsApproved)
                          AND (@SearchText IS NULL OR Username LIKE '%' + @SearchText + '%' OR MobileNumber LIKE '%' + @SearchText + '%');", connection))
                    {
                        countCmd.Parameters.AddWithValue("@SearchText", (object?)model.SearchText ?? DBNull.Value);
                        countCmd.Parameters.AddWithValue("@IsApproved", (object?)model.IsApproved ?? DBNull.Value);

                        totalRecords = Convert.ToInt32(await countCmd.ExecuteScalarAsync() ?? 0);
                    }

                    int pageNo = model.PageNo > 0 ? model.PageNo : 1;
                    int pageSize = model.PageSize > 0 ? model.PageSize : 50;
                    int offset = (pageNo - 1) * pageSize;

                    using (var cmd = new SqlCommand(@"
                        SELECT UserId, Username, MobileNumber, IsApproved, CreatedDate, LastLoginDate 
                        FROM dbo.Users 
                        WHERE IsDelete = 0
                          AND (@IsApproved IS NULL OR IsApproved = @IsApproved)
                          AND (@SearchText IS NULL OR Username LIKE '%' + @SearchText + '%' OR MobileNumber LIKE '%' + @SearchText + '%')
                        ORDER BY UserId DESC
                        OFFSET @Offset ROWS
                        FETCH NEXT @PageSize ROWS ONLY;", connection))
                    {
                        cmd.Parameters.AddWithValue("@Offset", offset);
                        cmd.Parameters.AddWithValue("@PageSize", pageSize);
                        cmd.Parameters.AddWithValue("@SearchText", (object?)model.SearchText ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@IsApproved", (object?)model.IsApproved ?? DBNull.Value);

                        using var reader = await cmd.ExecuteReaderAsync();
                        list = reader.DataReaderMapToList<UserItemRes>();
                    }
                }
                catch (Exception ex)
                {
                    strDBError = JsonConvert.SerializeObject(ex);
                }
                finally
                {
                    if (connection.State == ConnectionState.Open) await connection.CloseAsync();
                }
            }

            return new Tuple<List<UserItemRes>, int, string>(list, totalRecords, strDBError);
        }

        public async Task<Tuple<bool, int, string>> ToggleUserApprovalAsync(ToggleApprovalReq model)
        {
            string strDBError = string.Empty;
            bool success = false;

            using (var connection = new SqlConnection(_connectionString))
            {
                try
                {
                    await connection.OpenAsync();

                    bool newStatus;
                    if (model.IsApproved.HasValue)
                    {
                        newStatus = model.IsApproved.Value;
                    }
                    else
                    {
                        using var getCmd = new SqlCommand("SELECT IsApproved FROM dbo.Users WHERE UserId = @UserId;", connection);
                        getCmd.Parameters.AddWithValue("@UserId", model.UserId);
                        var currentVal = await getCmd.ExecuteScalarAsync();
                        newStatus = currentVal == null || !Convert.ToBoolean(currentVal);
                    }

                    using var updateCmd = new SqlCommand("UPDATE dbo.Users SET IsApproved = @IsApproved WHERE UserId = @UserId;", connection);
                    updateCmd.Parameters.AddWithValue("@UserId", model.UserId);
                    updateCmd.Parameters.AddWithValue("@IsApproved", newStatus ? 1 : 0);

                    int rows = await updateCmd.ExecuteNonQueryAsync();
                    success = rows > 0;
                }
                catch (Exception ex)
                {
                    strDBError = JsonConvert.SerializeObject(ex);
                }
                finally
                {
                    if (connection.State == ConnectionState.Open) await connection.CloseAsync();
                }
            }

            return new Tuple<bool, int, string>(success, success ? 1 : 0, strDBError);
        }
    }
}
