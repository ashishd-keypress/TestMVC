using System.Collections.Generic;
using System.Threading.Tasks;
using TestMVC.Interface;
using TestMVC.Models;

namespace TestMVC.Service
{
    public class SalesService : ISalesService
    {
        private readonly ISalesRepository _salesRepository;

        public SalesService(ISalesRepository salesRepository)
        {
            _salesRepository = salesRepository;
        }

        public Task<List<SalesInvoice>> GetInvoices(string? search = null)
        {
            return _salesRepository.GetInvoices(search);
        }

        public Task<SalesInvoice?> GetInvoiceById(long invoiceId)
        {
            return _salesRepository.GetInvoiceById(invoiceId);
        }

        public Task<long> CreateSalesInvoice(SalesInvoice invoice)
        {
            return _salesRepository.CreateSalesInvoice(invoice);
        }

        public Task<List<PaymentMethod>> GetPaymentMethods()
        {
            return _salesRepository.GetPaymentMethods();
        }

        public Task<decimal> GetTodaySalesAmount()
        {
            return _salesRepository.GetTodaySalesAmount();
        }

        public Task<List<SalesInvoice>> GetRecentInvoices(int count = 5)
        {
            return _salesRepository.GetRecentInvoices(count);
        }
    }
}
