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

        public Task<List<SalesInvoice>> GetInvoices(string? search = null, int? userId = null)
        {
            return _salesRepository.GetInvoices(search, userId);
        }

        public Task<SalesInvoice?> GetInvoiceById(long invoiceId, int? userId = null)
        {
            return _salesRepository.GetInvoiceById(invoiceId, userId);
        }

        public Task<long> CreateSalesInvoice(SalesInvoice invoice)
        {
            return _salesRepository.CreateSalesInvoice(invoice);
        }

        public Task<List<PaymentMethod>> GetPaymentMethods(int? userId = null)
        {
            return _salesRepository.GetPaymentMethods(userId);
        }

        public Task<PaymentMethod?> GetPaymentMethodById(int paymentMethodId, int? userId = null)
        {
            return _salesRepository.GetPaymentMethodById(paymentMethodId, userId);
        }

        public Task<(bool Success, string Message, PaymentMethod? PaymentMethod)> SavePaymentMethod(PaymentMethod paymentMethod)
        {
            return _salesRepository.SavePaymentMethod(paymentMethod);
        }

        public Task<(bool Success, string Message)> DeletePaymentMethod(int paymentMethodId, int? userId = null)
        {
            return _salesRepository.DeletePaymentMethod(paymentMethodId, userId);
        }

        public Task<decimal> GetTodaySalesAmount(int? userId = null)
        {
            return _salesRepository.GetTodaySalesAmount(userId);
        }

        public Task<List<SalesInvoice>> GetRecentInvoices(int count = 5, int? userId = null)
        {
            return _salesRepository.GetRecentInvoices(count, userId);
        }
    }
}
