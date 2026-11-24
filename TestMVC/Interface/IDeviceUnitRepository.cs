using System.Collections.Generic;
using System.Threading.Tasks;
using TestMVC.Models;

namespace TestMVC.Interface
{
    public interface IDeviceUnitRepository
    {
        Task<List<DeviceUnit>> GetDeviceUnits(string? search = null, byte? status = null, int? productId = null, int? userId = null);
        Task<DeviceUnit?> GetDeviceUnitById(long deviceUnitId, int? userId = null);
        Task<DeviceUnit?> GetDeviceByIMEI(string imei, int? userId = null);
        Task<List<DeviceUnit>> GetAvailableDevicesForProduct(int productId, int? userId = null);
        Task<bool> UpdateDeviceStatus(long deviceUnitId, byte status, string? notes = null);
        Task<int> GetInStockDeviceCount(int? userId = null);
        Task<decimal> GetTotalStockValue(int? userId = null);
    }

    public interface IDeviceUnitService
    {
        Task<List<DeviceUnit>> GetDeviceUnits(string? search = null, byte? status = null, int? productId = null, int? userId = null);
        Task<DeviceUnit?> GetDeviceUnitById(long deviceUnitId, int? userId = null);
        Task<DeviceUnit?> GetDeviceByIMEI(string imei, int? userId = null);
        Task<List<DeviceUnit>> GetAvailableDevicesForProduct(int productId, int? userId = null);
        Task<bool> UpdateDeviceStatus(long deviceUnitId, byte status, string? notes = null);
        Task<int> GetInStockDeviceCount(int? userId = null);
        Task<decimal> GetTotalStockValue(int? userId = null);
    }
}
