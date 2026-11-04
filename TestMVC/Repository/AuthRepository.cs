using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
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

        public AuthRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("DefaultConnection is missing in appsettings.json");

            DbInitializer.EnsureDatabaseSetup(_connectionString);
        }

        public async Task EnsureUserTableCreatedAsync()
        {
            DbInitializer.EnsureDatabaseSetup(_connectionString);
            await Task.CompletedTask;
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

                    using (var cmd = new SqlCommand(@"
                        SELECT UserId, Username, PasswordHash, PasswordSalt, MobileNumber 
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

                    // Update last login timestamp
                    using (var updateCmd = new SqlCommand("UPDATE dbo.Users SET LastLoginDate = GETDATE() WHERE UserId = @UserId;", connection))
                    {
                        updateCmd.Parameters.AddWithValue("@UserId", userId);
                        await updateCmd.ExecuteNonQueryAsync();
                    }

                    res.Success = true;
                    res.Message = "Login successful!";
                    res.Token = Guid.NewGuid().ToString("N");
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
    }
}
