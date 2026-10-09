using Microsoft.Data.SqlClient;
using System.Data;
using TestMVC.Interface;
using TestMVC.Models;

namespace TestMVC.Repository
{
    public class BrandRepository : IBrandRepository
    {
        private readonly string _connectionString;

        public BrandRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("DefaultConnection is missing in appsettings.json");
        }

        // Calls dbo.GetBrands
        public async Task<List<Brand>> GetAllBrandsAsync()
        {
            var list = new List<Brand>();

            using var con = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("dbo.GetBrands", con)
            {
                CommandType = CommandType.StoredProcedure
            };

            await con.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                list.Add(new Brand
                {
                    BrandId = Convert.ToInt32(reader["BrandId"]),
                    BrandName = reader["BrandName"].ToString() ?? "",
                    IsActive = reader["IsActive"] != DBNull.Value ? Convert.ToBoolean(reader["IsActive"]) : true,
                    CreatedDate = reader["CreatedDate"] != DBNull.Value ? Convert.ToDateTime(reader["CreatedDate"]) : null
                });
            }

            return list;
        }

        // Calls dbo.GetBrandById
        public async Task<Brand?> GetBrandByIdAsync(int brandId)
        {
            using var con = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("dbo.GetBrandById", con)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.Add(new SqlParameter("@BrandId", SqlDbType.Int) { Value = brandId });

            await con.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return new Brand
                {
                    BrandId = Convert.ToInt32(reader["BrandId"]),
                    BrandName = reader["BrandName"].ToString() ?? "",
                    IsActive = reader["IsActive"] != DBNull.Value ? Convert.ToBoolean(reader["IsActive"]) : true,
                    CreatedDate = reader["CreatedDate"] != DBNull.Value ? Convert.ToDateTime(reader["CreatedDate"]) : null
                };
            }

            return null;
        }

        // Calls dbo.AddBrand
        public async Task AddBrandAsync(Brand brand)
        {
            using var con = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("dbo.AddBrand", con)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.Add(new SqlParameter("@BrandName", SqlDbType.VarChar, 100) { Value = brand.BrandName });

            await con.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }

        // Calls dbo.UpdateBrand
        public async Task UpdateBrandAsync(Brand brand)
        {
            using var con = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("dbo.UpdateBrand", con)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.Add(new SqlParameter("@BrandId", SqlDbType.Int) { Value = brand.BrandId });
            cmd.Parameters.Add(new SqlParameter("@BrandName", SqlDbType.VarChar, 100) { Value = brand.BrandName });

            await con.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }

        // Calls dbo.DeleteBrand
        public async Task DeleteBrandAsync(int brandId)
        {
            using var con = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("dbo.DeleteBrand", con)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.Add(new SqlParameter("@BrandId", SqlDbType.Int) { Value = brandId });

            await con.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }
    }
}
