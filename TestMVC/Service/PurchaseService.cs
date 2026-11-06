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

        public Task<List<Purchase>> GetPurchases(string? search = null)
        {
            return _purchaseRepository.GetPurchases(search);
        }

        public Task<Purchase?> GetPurchaseById(long purchaseId)
        {
            return _purchaseRepository.GetPurchaseById(purchaseId);
        }

        public Task<long> SavePurchase(Purchase purchase)
        {
            return _purchaseRepository.SavePurchase(purchase);
        }
    }
}
