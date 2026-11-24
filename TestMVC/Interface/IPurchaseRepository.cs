using System.Collections.Generic;
using System.Threading.Tasks;
using TestMVC.Models;

namespace TestMVC.Interface
{
    public interface IPurchaseRepository
    {
        Task<List<Purchase>> GetPurchases(string? search = null, int? userId = null);
        Task<Purchase?> GetPurchaseById(long purchaseId, int? userId = null);
        Task<long> SavePurchase(Purchase purchase);
    }

    public interface IPurchaseService
    {
        Task<List<Purchase>> GetPurchases(string? search = null, int? userId = null);
        Task<Purchase?> GetPurchaseById(long purchaseId, int? userId = null);
        Task<long> SavePurchase(Purchase purchase);
    }
}
