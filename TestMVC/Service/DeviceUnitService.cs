using System.Collections.Generic;
using System.Threading.Tasks;
using TestMVC.Interface;
using TestMVC.Models;

namespace TestMVC.Service
{
    public class DeviceUnitService : IDeviceUnitService
    {
        private readonly IDeviceUnitRepository _deviceUnitRepository;

        public DeviceUnitService(IDeviceUnitRepository deviceUnitRepository)
        {
            _deviceUnitRepository = deviceUnitRepository;
        }

        public Task<List<DeviceUnit>> GetDeviceUnits(string? search = null, byte? status = null, int? productId = null, int? userId = null)
        {
            return _deviceUnitRepository.GetDeviceUnits(search, status, productId, userId);
        }

        public Task<DeviceUnit?> GetDeviceUnitById(long deviceUnitId, int? userId = null)
        {
            return _deviceUnitRepository.GetDeviceUnitById(deviceUnitId, userId);
        }

        public Task<DeviceUnit?> GetDeviceByIMEI(string imei, int? userId = null)
        {
            return _deviceUnitRepository.GetDeviceByIMEI(imei, userId);
        }

        public Task<List<DeviceUnit>> GetAvailableDevicesForProduct(int productId, int? userId = null)
        {
            return _deviceUnitRepository.GetAvailableDevicesForProduct(productId, userId);
        }

        public Task<bool> UpdateDeviceStatus(long deviceUnitId, byte status, string? notes = null)
        {
            return _deviceUnitRepository.UpdateDeviceStatus(deviceUnitId, status, notes);
        }

        public Task<int> GetInStockDeviceCount(int? userId = null)
        {
            return _deviceUnitRepository.GetInStockDeviceCount(userId);
        }

        public Task<decimal> GetTotalStockValue(int? userId = null)
        {
            return _deviceUnitRepository.GetTotalStockValue(userId);
        }
    }
}
