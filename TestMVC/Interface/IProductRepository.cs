using TestMVC.Models;

namespace TestMVC.Interface
{
    public interface IProductRepository
    {
        Task<List<Product>> GetProductsAsync(int pageNo, int pageSize, string? search);
        Task<Product?> GetProductByIdAsync(int productId);
        Task AddProductAsync(Product product);
        Task UpdateProductAsync(Product product);
        Task DeleteProductAsync(int productId);
        Task<List<Category>> GetCategoriesAsync();
        Task<List<Brand>> GetBrandsAsync();
    }
}
