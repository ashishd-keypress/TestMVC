using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using TestMVC.Interface;
using TestMVC.Models;

namespace TestMVC.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly IDashboardService _dashboardService;
        private readonly IDeviceUnitService _deviceUnitService;
        private readonly ISalesService _salesService;
        private readonly ICustomerService _customerService;
        private readonly ISupplierService _supplierService;

        public HomeController(
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

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> Index()
        {
            int? currentUserId = Utility.Helper.GetUserId(User);
            var (stats, _, _) = await _dashboardService.GetDashboardStats(currentUserId);

            var inStockDevices = await _deviceUnitService.GetInStockDeviceCount(currentUserId);
            var totalStockVal = await _deviceUnitService.GetTotalStockValue(currentUserId);
            var todaySales = await _salesService.GetTodaySalesAmount(currentUserId);
            var custCount = await _customerService.GetCustomerCount(currentUserId);
            var suppCount = await _supplierService.GetSupplierCount(currentUserId);
            var recentInvoices = await _salesService.GetRecentInvoices(5, currentUserId);
            var recentDevices = await _deviceUnitService.GetDeviceUnits(status: 1, userId: currentUserId);

            var viewModel = new DashboardViewModel
            {
                TotalProducts = stats.TotalProducts,
                TotalCategories = stats.TotalCategories,
                TotalBrands = stats.TotalBrands,
                TotalInStockDevices = inStockDevices,
                TotalStockValue = totalStockVal,
                TodaySalesAmount = todaySales,
                TotalCustomers = custCount,
                TotalSuppliers = suppCount,
                RecentInvoices = recentInvoices,
                RecentDevices = recentDevices.Take(5).ToList()
            };

            return View(viewModel);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
