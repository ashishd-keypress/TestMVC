using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using TestMVC.Interface;
using TestMVC.Models;

namespace TestMVC.Controllers
{
    [Authorize]
    public class InventoryController : Controller
    {
        private readonly IDeviceUnitService _deviceUnitService;
        private readonly IProductService _productService;

        public InventoryController(IDeviceUnitService deviceUnitService, IProductService productService)
        {
            _deviceUnitService = deviceUnitService;
            _productService = productService;
        }

        // GET: /Inventory
        [HttpGet]
        public async Task<IActionResult> Index(string? search, byte? status, int? productId)
        {
            var devices = await _deviceUnitService.GetDeviceUnits(search, status, productId);
            ViewBag.Search = search;
            ViewBag.SelectedStatus = status;
            ViewBag.SelectedProductId = productId;

            var (products, _, _) = await _productService.GetProduct(new Models.ReqRes.GetProductReq { PageSize = 1000, IsActive = true });
            ViewBag.Products = products;

            ViewBag.InStockCount = await _deviceUnitService.GetInStockDeviceCount();
            ViewBag.TotalStockValue = await _deviceUnitService.GetTotalStockValue();

            return View(devices);
        }

        // GET: /Inventory/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(long id)
        {
            var device = await _deviceUnitService.GetDeviceUnitById(id);
            if (device == null)
            {
                TempData["Error"] = "Device record not found.";
                return RedirectToAction(nameof(Index));
            }

            return View(device);
        }

        // POST: /Inventory/UpdateStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(long id, byte status, string? notes)
        {
            var success = await _deviceUnitService.UpdateDeviceStatus(id, status, notes);
            if (success)
            {
                TempData["Success"] = "Device status updated successfully.";
            }
            else
            {
                TempData["Error"] = "Failed to update device status.";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: /Inventory/SearchImei?imei=...
        [HttpGet]
        public async Task<IActionResult> SearchImei(string imei)
        {
            if (string.IsNullOrWhiteSpace(imei))
            {
                return Json(new { success = false, message = "Please enter an IMEI number." });
            }

            var device = await _deviceUnitService.GetDeviceByIMEI(imei);
            if (device == null)
            {
                return Json(new { success = false, message = $"IMEI '{imei}' not found in database." });
            }

            return Json(new
            {
                success = true,
                device = new
                {
                    deviceUnitId = device.DeviceUnitId,
                    productId = device.ProductId,
                    productName = device.ProductName,
                    brand = device.Brand,
                    category = device.Category,
                    ram = device.RAM,
                    storage = device.Storage,
                    color = device.Color,
                    imei1 = device.IMEI1,
                    imei2 = device.IMEI2,
                    currentStatus = device.CurrentStatus,
                    statusName = device.StatusName,
                    costPrice = device.CostPrice,
                    sellingPrice = device.SellingPrice
                }
            });
        }

        // GET: /Inventory/AvailableByProduct?productId=...
        [HttpGet]
        public async Task<IActionResult> AvailableByProduct(int productId)
        {
            var devices = await _deviceUnitService.GetAvailableDevicesForProduct(productId);
            return Json(new
            {
                success = true,
                devices = devices
            });
        }
    }
}
