using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using TestMVC.Interface;
using Utility;

namespace TestMVC.Controllers.Api
{
    [ApiController]
    [Route("api/Dashboard")]
    public class DashboardApiController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;
        private readonly IDeviceUnitService _deviceUnitService;
        private readonly ISalesService _salesService;
        private readonly ICustomerService _customerService;
        private readonly ISupplierService _supplierService;

        public DashboardApiController(
            IDashboardService dashboardService,
            IDeviceUnitService deviceUnitService,
            ISalesService salesService,
            ICustomerService customerService,
            ISupplierService supplierService)
        {
            _dashboardService = dashboardService;
            _deviceUnitService = deviceUnitService;
            _salesService = salesService;
            _customerService = customerService;
            _supplierService = supplierService;
        }

        [HttpGet]
        [Route("dashboard_summary_get")]
        public async Task<IActionResult> GetSummary()
        {
            int? currentUserId = Helper.GetUserId(User);
            var (stats, _, dbError) = await _dashboardService.GetDashboardStats(currentUserId);
            var inStockDevices = await _deviceUnitService.GetInStockDeviceCount(currentUserId);
            var totalStockVal = await _deviceUnitService.GetTotalStockValue(currentUserId);
            var todaySales = await _salesService.GetTodaySalesAmount(currentUserId);
            var custCount = await _customerService.GetCustomerCount(currentUserId);
            var suppCount = await _supplierService.GetSupplierCount(currentUserId);

            return Ok(new
            {
                success = string.IsNullOrEmpty(dbError),
                data = new
                {
                    totalProducts = stats.TotalProducts,
                    totalCategories = stats.TotalCategories,
                    totalBrands = stats.TotalBrands,
                    totalInStockDevices = inStockDevices,
                    totalStockValue = totalStockVal,
                    todaySalesAmount = todaySales,
                    totalCustomers = custCount,
                    totalSuppliers = suppCount
                }
            });
        }
    }
}
