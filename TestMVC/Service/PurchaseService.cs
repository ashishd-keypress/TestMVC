using System.Collections.Generic;
using System.Threading.Tasks;
using TestMVC.Interface;
using TestMVC.Models;

namespace TestMVC.Service
{
    public class PurchaseService : IPurchaseService
    {
        private readonly IPurchaseRepository _purchaseRepository;

        public PurchaseService(IPurchaseRepository purchaseRepository)
        {
            _purchaseRepository = purchaseRepository;
        }

        public Task<List<Purchase>> GetPurchases(string? search = null, int? userId = null)
        {
            return _purchaseRepository.GetPurchases(search, userId);
        }

        public Task<Purchase?> GetPurchaseById(long purchaseId, int? userId = null)
        {
            return _purchaseRepository.GetPurchaseById(purchaseId, userId);
        }

        public Task<long> SavePurchase(Purchase purchase)
        {
            return _purchaseRepository.SavePurchase(purchase);
        }
    }
}
