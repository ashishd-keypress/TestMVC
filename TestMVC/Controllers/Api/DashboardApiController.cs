using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using TestMVC.Interface;
using TestMVC.Models.ReqRes;
using Utility;

namespace TestMVC.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;
        private readonly IDeviceUnitService _deviceUnitService;
        private readonly ISalesService _salesService;
        private readonly ICustomerService _customerService;
        private readonly ISupplierService _supplierService;

        public DashboardController(
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
        [Route("")]
        [Route("dashboard_summary_get")]
        [Route("summary")]
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

        [HttpGet]
        [HttpPost]
        [Route("get-dashboard-stats")]
        public async Task<IActionResult> GetDashboardStats()
        {
            DateTime reqDateTime = DateTime.UtcNow;
            string StrSystemError = "NO SYSTEM ERROR.";
            string StrDBError = string.Empty;
            HttpStatusCode statusCode = HttpStatusCode.OK;
            TranStatus tranStatus = new TranStatus() { ReturnMessage = "Success.", Code = 0 };
            Dictionary<string, object?> dctData = new Dictionary<string, object?>();
            GetDashboardStatsRes response = new GetDashboardStatsRes();
            int TotalRecords = 0;

            try
            {
                int? currentUserId = Helper.GetUserId(User);
                (response, TotalRecords, StrDBError) = await _dashboardService.GetDashboardStats(currentUserId);

                if (!string.IsNullOrEmpty(StrDBError))
                {
                    tranStatus.ReturnMessage = "Something went wrong(DB).";
                    tranStatus.Code = 2;
                }
            }
            catch (Exception ex)
            {
                tranStatus.Code = 2;
                tranStatus.ReturnMessage = "Something went wrong.";
                statusCode = HttpStatusCode.BadRequest;
                StrSystemError = JsonConvert.SerializeObject(ex);
            }
            finally
            {
                dctData.Add("Data", response);
                dctData.Add("TotalRecords", TotalRecords);
                dctData.Add("Status", tranStatus);

                string message = "*******************************************************************" + Environment.NewLine
                                 + "Request UTC: " + reqDateTime.ToString("yyyy-MM-dd HH:mm:ss.fff") + Environment.NewLine
                                 + "Response UTC: " + DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff") + Environment.NewLine
                                 + "Response: " + JsonConvert.SerializeObject(dctData) + Environment.NewLine
                                 + "Message: " + StrSystemError + Environment.NewLine
                                 + "DataBase Error: " + StrDBError + Environment.NewLine;

                string path = string.Format("\\Dashboard\\GetDashboardStats\\{0}", DateTime.UtcNow.ToString("yyyyMMdd"));
                Helper.WriteToFile(message, path, DateTime.UtcNow.ToString("hh_00 tt"), false);
            }
            return this.StatusCode(Convert.ToInt32(statusCode), dctData);
        }
    }
}
