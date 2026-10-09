using TestMVC.Models;

namespace TestMVC.Interface
{
    public interface IProductRepository
    {
        Task<List<Product>> GetProductsAsync(int pageNo, int pageSize, string? search);
        Task AddProductAsync(Product product);
    }
}
