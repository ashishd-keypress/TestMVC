using System;
using System.Threading.Tasks;
using TestMVC.Interface;
using TestMVC.Models.ReqRes;

namespace TestMVC.Service
{
    public class DashboardService : IDashboardService
    {
        private readonly IProductRepository _productRepository;
        private readonly IBrandRepository _brandRepository;
        private readonly ICategoryRepository _categoryRepository;

        public DashboardService(
            IProductRepository productRepository,
            IBrandRepository brandRepository,
            ICategoryRepository categoryRepository)
        {
            _productRepository = productRepository;
            _brandRepository = brandRepository;
            _categoryRepository = categoryRepository;
        }

        public async Task<Tuple<GetDashboardStatsRes, int, string>> GetDashboardStats(int? userId = null)
        {
            string dbError = string.Empty;
            var stats = new GetDashboardStatsRes();

            try
            {
                var (prodCount, _, prodErr) = await _productRepository.GetActiveProductCount(userId);
                var (brandCount, _, brandErr) = await _brandRepository.GetActiveBrandCount(userId);
                var (catCount, _, catErr) = await _categoryRepository.GetActiveCategoryCount(userId);

                stats.TotalProducts = prodCount;
                stats.TotalBrands = brandCount;
                stats.TotalCategories = catCount;

                if (!string.IsNullOrEmpty(prodErr)) dbError += prodErr + "; ";
                if (!string.IsNullOrEmpty(brandErr)) dbError += brandErr + "; ";
                if (!string.IsNullOrEmpty(catErr)) dbError += catErr + "; ";
            }
            catch (Exception ex)
            {
                dbError = ex.Message;
            }

            return new Tuple<GetDashboardStatsRes, int, string>(stats, 1, dbError);
        }
    }
}
