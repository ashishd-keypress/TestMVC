using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TestMVC.Interface;

namespace TestMVC.Controllers.Api
{
    public class UpdateDeviceStatusRequest
    {
        public long Id { get; set; }
        public byte Status { get; set; }
        public string? Notes { get; set; }
    }

    [ApiController]
    [Route("api/[controller]")]
    public class InventoryController : ControllerBase
    {
        private readonly IDeviceUnitService _deviceUnitService;

        public InventoryController(IDeviceUnitService deviceUnitService)
        {
            _deviceUnitService = deviceUnitService;
        }

        [HttpGet]
        [Route("")]
        [Route("inventory_list_get")]
        [Route("search")]
        public async Task<IActionResult> Search([FromQuery] string? search, [FromQuery] byte? status, [FromQuery] int? productId)
        {
            int? currentUserId = Utility.Helper.GetUserId(User);
            var devices = await _deviceUnitService.GetDeviceUnits(search, status, productId, currentUserId);
            var inStockCount = await _deviceUnitService.GetInStockDeviceCount(currentUserId);
            var totalStockValue = await _deviceUnitService.GetTotalStockValue(currentUserId);

            return Ok(new
            {
                success = true,
                data = devices.Select(d => new
                {
                    deviceUnitId = d.DeviceUnitId,
                    productId = d.ProductId,
                    productName = d.ProductName,
                    brand = d.Brand,
                    category = d.Category,
                    ram = d.RAM,
                    storage = d.Storage,
                    color = d.Color,
                    imei1 = d.IMEI1,
                    imei2 = d.IMEI2,
                    serialNumber = d.SerialNumber,
                    costPrice = d.CostPrice ?? 0,
                    costPriceFormatted = (d.CostPrice ?? 0).ToString("N2"),
                    sellingPrice = d.SellingPrice ?? 0,
                    sellingPriceFormatted = (d.SellingPrice ?? 0).ToString("N2"),
                    currentStatus = d.CurrentStatus,
                    statusName = d.StatusName,
                    createdDate = d.CreatedDate.ToString("yyyy-MM-dd hh:mm tt")
                }),
                inStockCount,
                totalStockValue,
                totalStockValueFormatted = totalStockValue.ToString("N2"),
                totalRecords = devices.Count
            });
        }

        [HttpGet]
        [Route("inventory_detail_get")]
        [Route("{id:long}")]
        public async Task<IActionResult> GetById([FromQuery(Name = "id")] long? queryId = null, long? id = null)
        {
            long targetId = (queryId ?? 0) > 0 ? queryId!.Value : (id ?? 0);
            if (targetId <= 0) return BadRequest(new { success = false, message = "Invalid device unit ID." });

            int? currentUserId = Utility.Helper.GetUserId(User);
            var device = await _deviceUnitService.GetDeviceUnitById(targetId, currentUserId);
            if (device == null)
            {
                return NotFound(new { success = false, message = "Device record not found." });
            }

            return Ok(new { success = true, data = device });
        }

        [HttpGet]
        [Route("inventory_imei_search")]
        [Route("search-imei")]
        [Route("SearchImei")]
        public async Task<IActionResult> SearchImei([FromQuery] string imei)
        {
            if (string.IsNullOrWhiteSpace(imei))
            {
                return BadRequest(new { success = false, message = "Please enter an IMEI number." });
            }

            int? currentUserId = Utility.Helper.GetUserId(User);
            var device = await _deviceUnitService.GetDeviceByIMEI(imei, currentUserId);
            if (device == null)
            {
                return NotFound(new { success = false, message = $"IMEI '{imei}' not found in database." });
            }

            return Ok(new
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
                    sellingPrice = device.SellingPrice ?? 0
                }
            });
        }

        [HttpGet]
        [Route("inventory_available_devices_get")]
        [Route("available-by-product")]
        [Route("AvailableByProduct")]
        public async Task<IActionResult> AvailableByProduct([FromQuery] int productId)
        {
            int? currentUserId = Utility.Helper.GetUserId(User);
            var devices = await _deviceUnitService.GetAvailableDevicesForProduct(productId, currentUserId);
            return Ok(new
            {
                success = true,
                devices = devices.Select(d => new
                {
                    deviceUnitId = d.DeviceUnitId,
                    imei1 = d.IMEI1,
                    imei2 = d.IMEI2,
                    serialNumber = d.SerialNumber
                })
            });
        }

        [HttpPut]
        [HttpPost]
        [Route("inventory_status_update")]
        [Route("update-status/{id:long}")]
        [Route("update-status")]
        public async Task<IActionResult> UpdateStatus([FromBody] UpdateDeviceStatusRequest request, [FromQuery(Name = "id")] long? queryId = null, long? id = null)
        {
            long targetId = (queryId ?? 0) > 0 ? queryId!.Value : ((id ?? 0) > 0 ? id!.Value : (request?.Id ?? 0));
            if (targetId <= 0 || request == null || request.Status < 1 || request.Status > 6)
            {
                return BadRequest(new { success = false, message = "Invalid device unit ID or status." });
            }

            int? currentUserId = Utility.Helper.GetUserId(User);
            var existing = await _deviceUnitService.GetDeviceUnitById(targetId, currentUserId);
            if (existing == null)
            {
                return NotFound(new { success = false, message = "Device record not found." });
            }

            var success = await _deviceUnitService.UpdateDeviceStatus(targetId, request.Status, request.Notes);
            if (!success)
            {
                return BadRequest(new { success = false, message = "Failed to update device status." });
            }

            var updated = await _deviceUnitService.GetDeviceUnitById(targetId, currentUserId);
            return Ok(new
            {
                success = true,
                message = "Device status updated successfully.",
                data = new
                {
                    deviceUnitId = targetId,
                    currentStatus = updated?.CurrentStatus ?? request.Status,
                    statusName = updated?.StatusName ?? "",
                    notes = updated?.Notes ?? request.Notes
                }
            });
        }
    }
}
