using System.Collections.Generic;
using System.Threading.Tasks;
using TestMVC.Interface;
using TestMVC.Models;

namespace TestMVC.Service
{
    public class SupplierService : ISupplierService
    {
        private readonly ISupplierRepository _supplierRepository;

        public SupplierService(ISupplierRepository supplierRepository)
        {
            _supplierRepository = supplierRepository;
        }

        public Task<List<Supplier>> GetSuppliers(string? search = null, int? userId = null)
        {
            return _supplierRepository.GetSuppliers(search, userId);
        }

        public Task<Supplier?> GetSupplierById(long supplierId, int? userId = null)
        {
            return _supplierRepository.GetSupplierById(supplierId, userId);
        }

        public Task<bool> SaveSupplier(Supplier supplier)
        {
            return _supplierRepository.SaveSupplier(supplier);
        }

        public Task<bool> DeleteSupplier(long supplierId, int? userId = null)
        {
            return _supplierRepository.DeleteSupplier(supplierId, userId);
        }

        public Task<bool> ToggleStatus(long supplierId, int? userId = null)
        {
            return _supplierRepository.ToggleStatus(supplierId, userId);
        }

        public Task<int> GetSupplierCount(int? userId = null)
        {
            return _supplierRepository.GetSupplierCount(userId);
        }
    }
}
