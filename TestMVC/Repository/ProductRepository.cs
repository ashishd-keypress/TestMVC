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

            cmd.Parameters.AddWithValue("@PageNo", pageNo);       // 1-based
            cmd.Parameters.AddWithValue("@PageSize", pageSize);
            cmd.Parameters.AddWithValue("@Search", (object?)search ?? DBNull.Value);

            await con.OpenAsync();
            using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                list.Add(new Product
                {
                    ProductId = Convert.ToInt32(reader["ProductId"]),
                    ProductName = reader["ProductName"].ToString() ?? "",
                    Price = Convert.ToDecimal(reader["Price"]),
                    Quantity = Convert.ToInt32(reader["Quantity"])
                });
            }

            return list;
        }

        // Calls dbo.AddProduct (validation is inside the stored procedure)
        public async Task AddProductAsync(Product product)
        {
            using var con = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("dbo.AddProduct", con)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.AddWithValue("@ProductName", product.ProductName);
            cmd.Parameters.AddWithValue("@Price", product.Price);
            cmd.Parameters.AddWithValue("@Quantity", product.Quantity);

            await con.OpenAsync();
            await cmd.ExecuteNonQueryAsync();
        }


    }
}
