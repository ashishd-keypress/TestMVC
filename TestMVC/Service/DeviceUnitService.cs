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

        public Task<List<DeviceUnit>> GetDeviceUnits(string? search = null, byte? status = null, int? productId = null)
        {
            return _deviceUnitRepository.GetDeviceUnits(search, status, productId);
        }

        public Task<DeviceUnit?> GetDeviceUnitById(long deviceUnitId)
        {
            return _deviceUnitRepository.GetDeviceUnitById(deviceUnitId);
        }

        public Task<DeviceUnit?> GetDeviceByIMEI(string imei)
        {
            return _deviceUnitRepository.GetDeviceByIMEI(imei);
        }

        public Task<List<DeviceUnit>> GetAvailableDevicesForProduct(int productId)
        {
            return _deviceUnitRepository.GetAvailableDevicesForProduct(productId);
        }

        public Task<bool> UpdateDeviceStatus(long deviceUnitId, byte status, string? notes = null)
        {
            return _deviceUnitRepository.UpdateDeviceStatus(deviceUnitId, status, notes);
        }

        public Task<int> GetInStockDeviceCount()
        {
            return _deviceUnitRepository.GetInStockDeviceCount();
        }

        public Task<decimal> GetTotalStockValue()
        {
            return _deviceUnitRepository.GetTotalStockValue();
        }
    }
}
