using System.Collections.Generic;
using System.Threading.Tasks;
using TestMVC.Models;

namespace TestMVC.Interface
{
    public interface ISupplierRepository
    {
        Task<List<Supplier>> GetSuppliers(string? search = null, int? userId = null);
        Task<Supplier?> GetSupplierById(long supplierId, int? userId = null);
        Task<bool> SaveSupplier(Supplier supplier);
        Task<bool> DeleteSupplier(long supplierId, int? userId = null);
        Task<int> GetSupplierCount(int? userId = null);
        Task<bool> ToggleStatus(long supplierId, int? userId = null);
    }

    public interface ISupplierService
    {
        Task<List<Supplier>> GetSuppliers(string? search = null, int? userId = null);
        Task<Supplier?> GetSupplierById(long supplierId, int? userId = null);
        Task<bool> SaveSupplier(Supplier supplier);
        Task<bool> DeleteSupplier(long supplierId, int? userId = null);
        Task<int> GetSupplierCount(int? userId = null);
        Task<bool> ToggleStatus(long supplierId, int? userId = null);
    }
}
