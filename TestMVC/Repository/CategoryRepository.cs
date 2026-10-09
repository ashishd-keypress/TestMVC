using Microsoft.Data.SqlClient;
using System.Data;
using TestMVC.Interface;
using TestMVC.Models;

namespace TestMVC.Repository
{
    public class CategoryRepository : ICategoryRepository
    {
        private readonly string _connectionString;

        public CategoryRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("DefaultConnection is missing in appsettings.json");
        }

        // Calls dbo.GetCategories
        public async Task<List<Category>> GetAllCategoriesAsync()
        {
            var list = new List<Category>();

            using var con = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("dbo.GetCategories", con)
            {
                CommandType = CommandType.StoredProcedure
            };

            await con.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                list.Add(new Category
                {
                    CategoryId = Convert.ToInt32(reader["CategoryId"]),
                    CategoryName = reader["CategoryName"].ToString() ?? "",
                    IsActive = reader["IsActive"] != DBNull.Value ? Convert.ToBoolean(reader["IsActive"]) : true,
                    CreatedDate = reader["CreatedDate"] != DBNull.Value ? Convert.ToDateTime(reader["CreatedDate"]) : null
                });
            }

            return list;
        }

        // Calls dbo.GetCategoryById
        public async Task<Category?> GetCategoryByIdAsync(int categoryId)
        {
            using var con = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("dbo.GetCategoryById", con)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.Add(new SqlParameter("@CategoryId", SqlDbType.Int) { Value = categoryId });

            await con.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return new Category
                {
                    CategoryId = Convert.ToInt32(reader["CategoryId"]),
                    CategoryName = reader["CategoryName"].ToString() ?? "",
                    IsActive = reader["IsActive"] != DBNull.Value ? Convert.ToBoolean(reader["IsActive"]) : true,
                    CreatedDate = reader["CreatedDate"] != DBNull.Value ? Convert.ToDateTime(reader["CreatedDate"]) : null
                };
            }

            return null;
        }

        // Calls dbo.AddCategory
        public async Task AddCategoryAsync(Category category)
        {
            using var con = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("dbo.AddCategory", con)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.Add(new SqlParameter("@CategoryName", SqlDbType.VarChar, 100) { Value = category.CategoryName });

            await con.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }

        // Calls dbo.UpdateCategory
        public async Task UpdateCategoryAsync(Category category)
        {
            using var con = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("dbo.UpdateCategory", con)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.Add(new SqlParameter("@CategoryId", SqlDbType.Int) { Value = category.CategoryId });
            cmd.Parameters.Add(new SqlParameter("@CategoryName", SqlDbType.VarChar, 100) { Value = category.CategoryName });

            await con.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }

        // Calls dbo.DeleteCategory
        public async Task DeleteCategoryAsync(int categoryId)
        {
            using var con = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("dbo.DeleteCategory", con)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.Add(new SqlParameter("@CategoryId", SqlDbType.Int) { Value = categoryId });

            await con.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }
    }
}
