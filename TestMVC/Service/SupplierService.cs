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

        public Task<List<Supplier>> GetSuppliers(string? search = null)
        {
            return _supplierRepository.GetSuppliers(search);
        }

        public Task<Supplier?> GetSupplierById(long supplierId)
        {
            return _supplierRepository.GetSupplierById(supplierId);
        }

        public Task<bool> SaveSupplier(Supplier supplier)
        {
            return _supplierRepository.SaveSupplier(supplier);
        }

        public Task<bool> DeleteSupplier(long supplierId)
        {
            return _supplierRepository.DeleteSupplier(supplierId);
        }

        public Task<bool> ToggleStatus(long supplierId)
        {
            return _supplierRepository.ToggleStatus(supplierId);
        }

        public Task<int> GetSupplierCount()
        {
            return _supplierRepository.GetSupplierCount();
        }
    }
}
