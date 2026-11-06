using System.Collections.Generic;
using System.Threading.Tasks;
using TestMVC.Models;

namespace TestMVC.Interface
{
    public interface ISupplierRepository
    {
        Task<List<Supplier>> GetSuppliers(string? search = null);
        Task<Supplier?> GetSupplierById(long supplierId);
        Task<bool> SaveSupplier(Supplier supplier);
        Task<bool> DeleteSupplier(long supplierId);
        Task<int> GetSupplierCount();
        Task<bool> ToggleStatus(long supplierId);
    }

    public interface ISupplierService
    {
        Task<List<Supplier>> GetSuppliers(string? search = null);
        Task<Supplier?> GetSupplierById(long supplierId);
        Task<bool> SaveSupplier(Supplier supplier);
        Task<bool> DeleteSupplier(long supplierId);
        Task<int> GetSupplierCount();
        Task<bool> ToggleStatus(long supplierId);
    }
}
