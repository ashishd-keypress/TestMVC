using System.Collections.Generic;
using System.Threading.Tasks;
using TestMVC.Models;

namespace TestMVC.Interface
{
    public interface ISalesRepository
    {
        Task<List<SalesInvoice>> GetInvoices(string? search = null);
        Task<SalesInvoice?> GetInvoiceById(long invoiceId);
        Task<long> CreateSalesInvoice(SalesInvoice invoice);
        Task<List<PaymentMethod>> GetPaymentMethods();
        Task<decimal> GetTodaySalesAmount();
        Task<List<SalesInvoice>> GetRecentInvoices(int count = 5);
    }

    public interface ISalesService
    {
        Task<List<SalesInvoice>> GetInvoices(string? search = null);
        Task<SalesInvoice?> GetInvoiceById(long invoiceId);
        Task<long> CreateSalesInvoice(SalesInvoice invoice);
        Task<List<PaymentMethod>> GetPaymentMethods();
        Task<decimal> GetTodaySalesAmount();
        Task<List<SalesInvoice>> GetRecentInvoices(int count = 5);
    }

    public class PaymentMethod
    {
        public int PaymentMethodId { get; set; }
        public string MethodName { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }
}
