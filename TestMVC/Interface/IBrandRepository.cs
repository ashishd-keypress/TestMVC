using TestMVC.Models;

namespace TestMVC.Interface
{
    public interface IBrandRepository
    {
        Task<List<Brand>> GetAllBrandsAsync();
        Task<Brand?> GetBrandByIdAsync(int brandId);
        Task AddBrandAsync(Brand brand);
        Task UpdateBrandAsync(Brand brand);
        Task DeleteBrandAsync(int brandId);
    }
}
