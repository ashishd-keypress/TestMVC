using Microsoft.Data.SqlClient;
using System.Data;
using TestMVC.Interface;
using TestMVC.Models;

namespace TestMVC.Repository
{
    public class ProductRepository : IProductRepository
    {
        private readonly string _connectionString;

        public ProductRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("DefaultConnection is missing in appsettings.json");
        }

        // Calls dbo.GetProduct (paging + search)
        public async Task<List<Product>> GetProductsAsync(int pageNo, int pageSize, string? search)
        {
            var list = new List<Product>();

            using var con = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("dbo.GetProduct", con)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.Add(new SqlParameter("@PageNumber", SqlDbType.Int) { Value = pageNo });
            cmd.Parameters.Add(new SqlParameter("@PageSize", SqlDbType.Int) { Value = pageSize });
            cmd.Parameters.Add(new SqlParameter("@SearchText", SqlDbType.VarChar, 100) { Value = (object?)search ?? DBNull.Value });
            cmd.Parameters.Add(new SqlParameter("@Category", SqlDbType.VarChar, 100) { Value = DBNull.Value });
            cmd.Parameters.Add(new SqlParameter("@Brand", SqlDbType.VarChar, 100) { Value = DBNull.Value });
            cmd.Parameters.Add(new SqlParameter("@Price", SqlDbType.Decimal) { Precision = 18, Scale = 2, Value = DBNull.Value });
            cmd.Parameters.Add(new SqlParameter("@OrderBy", SqlDbType.VarChar, 100) { Value = DBNull.Value });
            cmd.Parameters.Add(new SqlParameter("@Order", SqlDbType.VarChar, 10) { Value = DBNull.Value });

            var totalRecordsParam = new SqlParameter("@TotalRecords", SqlDbType.Int) { Direction = ParameterDirection.Output };
            var messageParam = new SqlParameter("@Message", SqlDbType.VarChar, 100) { Direction = ParameterDirection.Output };
            var codeParam = new SqlParameter("@Code", SqlDbType.Int) { Direction = ParameterDirection.Output };

            cmd.Parameters.Add(totalRecordsParam);
            cmd.Parameters.Add(messageParam);
            cmd.Parameters.Add(codeParam);

            await con.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                list.Add(new Product
                {
                    ProductId = reader["ProductId"] != DBNull.Value ? Convert.ToInt32(reader["ProductId"]) : 0,
                    Name = reader["Name"] != DBNull.Value ? reader["Name"].ToString() ?? "" : "",
                    Brand = reader["Brand"] != DBNull.Value ? reader["Brand"].ToString() : null,
                    Category = reader["Category"] != DBNull.Value ? reader["Category"].ToString() : null,
                    BrandId = reader["BrandId"] != DBNull.Value ? Convert.ToInt32(reader["BrandId"]) : 0,
                    CategoryId = reader["CategoryId"] != DBNull.Value ? Convert.ToInt32(reader["CategoryId"]) : 0,
                    Price = reader["Price"] != DBNull.Value ? Convert.ToDecimal(reader["Price"]) : 0,
                    Size = reader["Size"] != DBNull.Value ? reader["Size"].ToString() : null,
                    CreatedDate = reader["CreatedDate"] != DBNull.Value ? Convert.ToDateTime(reader["CreatedDate"]) : null
                });
            }

            return list;
        }

        // Calls dbo.GetProductById
        public async Task<Product?> GetProductByIdAsync(int productId)
        {
            using var con = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("dbo.GetProductById", con)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.Add(new SqlParameter("@ProductId", SqlDbType.Int) { Value = productId });

            await con.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return new Product
                {
                    ProductId = reader["ProductId"] != DBNull.Value ? Convert.ToInt32(reader["ProductId"]) : 0,
                    Name = reader["Name"] != DBNull.Value ? reader["Name"].ToString() ?? "" : "",
                    Brand = reader["Brand"] != DBNull.Value ? reader["Brand"].ToString() : null,
                    Category = reader["Category"] != DBNull.Value ? reader["Category"].ToString() : null,
                    BrandId = reader["BrandId"] != DBNull.Value ? Convert.ToInt32(reader["BrandId"]) : 0,
                    CategoryId = reader["CategoryId"] != DBNull.Value ? Convert.ToInt32(reader["CategoryId"]) : 0,
                    Price = reader["Price"] != DBNull.Value ? Convert.ToDecimal(reader["Price"]) : 0,
                    Size = reader["Size"] != DBNull.Value ? reader["Size"].ToString() : null,
                    CreatedDate = reader["CreatedDate"] != DBNull.Value ? Convert.ToDateTime(reader["CreatedDate"]) : null
                };
            }

            return null;
        }

        // Calls dbo.GetCategories
        public async Task<List<Category>> GetCategoriesAsync()
        {
            var categories = new List<Category>();

            using var con = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("dbo.GetCategories", con)
            {
                CommandType = CommandType.StoredProcedure
            };

            await con.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                categories.Add(new Category
                {
                    CategoryId = Convert.ToInt32(reader["CategoryId"]),
                    CategoryName = reader["CategoryName"].ToString() ?? ""
                });
            }

            return categories;
        }

        // Calls dbo.GetBrands
        public async Task<List<Brand>> GetBrandsAsync()
        {
            var brands = new List<Brand>();

            using var con = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("dbo.GetBrands", con)
            {
                CommandType = CommandType.StoredProcedure
            };

            await con.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                brands.Add(new Brand
                {
                    BrandId = Convert.ToInt32(reader["BrandId"]),
                    BrandName = reader["BrandName"].ToString() ?? ""
                });
            }

            return brands;
        }

        // Calls dbo.AddProduct
        public async Task AddProductAsync(Product product)
        {
            using var con = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("dbo.AddProduct", con)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.Add(new SqlParameter("@Name", SqlDbType.VarChar, 500) { Value = product.Name });
            cmd.Parameters.Add(new SqlParameter("@BrandId", SqlDbType.Int) { Value = product.BrandId });
            cmd.Parameters.Add(new SqlParameter("@CategoryId", SqlDbType.Int) { Value = product.CategoryId });
            cmd.Parameters.Add(new SqlParameter("@Price", SqlDbType.Decimal) { Precision = 18, Scale = 2, Value = product.Price });
            cmd.Parameters.Add(new SqlParameter("@Size", SqlDbType.VarChar, 100) { Value = (object?)product.Size ?? DBNull.Value });
            cmd.Parameters.Add(new SqlParameter("@CreatedBY", SqlDbType.Int) { Value = 1 });

            await con.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }

        // Calls dbo.UpdateProduct
        public async Task UpdateProductAsync(Product product)
        {
            using var con = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("dbo.UpdateProduct", con)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.Add(new SqlParameter("@ProductId", SqlDbType.Int) { Value = product.ProductId });
            cmd.Parameters.Add(new SqlParameter("@Name", SqlDbType.VarChar, 500) { Value = product.Name });
            cmd.Parameters.Add(new SqlParameter("@BrandId", SqlDbType.Int) { Value = product.BrandId });
            cmd.Parameters.Add(new SqlParameter("@CategoryId", SqlDbType.Int) { Value = product.CategoryId });
            cmd.Parameters.Add(new SqlParameter("@Price", SqlDbType.Decimal) { Precision = 18, Scale = 2, Value = product.Price });
            cmd.Parameters.Add(new SqlParameter("@Size", SqlDbType.VarChar, 100) { Value = (object?)product.Size ?? DBNull.Value });
            cmd.Parameters.Add(new SqlParameter("@ModifiedBy", SqlDbType.Int) { Value = 1 });

            await con.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }

        // Calls dbo.DeleteProduct
        public async Task DeleteProductAsync(int productId)
        {
            using var con = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("dbo.DeleteProduct", con)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.Add(new SqlParameter("@ProductId", SqlDbType.Int) { Value = productId });
            cmd.Parameters.Add(new SqlParameter("@ModifiedBy", SqlDbType.Int) { Value = 1 });

            await con.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }
    }
}
