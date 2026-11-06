using System.Collections.Generic;
using System.Threading.Tasks;
using TestMVC.Models;

namespace TestMVC.Interface
{
    public interface IDeviceUnitRepository
    {
        Task<List<DeviceUnit>> GetDeviceUnits(string? search = null, byte? status = null, int? productId = null);
        Task<DeviceUnit?> GetDeviceUnitById(long deviceUnitId);
        Task<DeviceUnit?> GetDeviceByIMEI(string imei);
        Task<List<DeviceUnit>> GetAvailableDevicesForProduct(int productId);
        Task<bool> UpdateDeviceStatus(long deviceUnitId, byte status, string? notes = null);
        Task<int> GetInStockDeviceCount();
        Task<decimal> GetTotalStockValue();
    }

    public interface IDeviceUnitService
    {
        Task<List<DeviceUnit>> GetDeviceUnits(string? search = null, byte? status = null, int? productId = null);
        Task<DeviceUnit?> GetDeviceUnitById(long deviceUnitId);
        Task<DeviceUnit?> GetDeviceByIMEI(string imei);
        Task<List<DeviceUnit>> GetAvailableDevicesForProduct(int productId);
        Task<bool> UpdateDeviceStatus(long deviceUnitId, byte status, string? notes = null);
        Task<int> GetInStockDeviceCount();
        Task<decimal> GetTotalStockValue();
    }
}
