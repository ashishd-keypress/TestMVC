using System.Collections.Generic;
using System.Threading.Tasks;
using TestMVC.Models;

namespace TestMVC.Interface
{
    public interface ISalesRepository
    {
        Task<List<SalesInvoice>> GetInvoices(string? search = null, int? userId = null);
        Task<SalesInvoice?> GetInvoiceById(long invoiceId, int? userId = null);
        Task<long> CreateSalesInvoice(SalesInvoice invoice);
        Task<List<PaymentMethod>> GetPaymentMethods();
        Task<decimal> GetTodaySalesAmount(int? userId = null);
        Task<List<SalesInvoice>> GetRecentInvoices(int count = 5, int? userId = null);
    }

    public interface ISalesService
    {
        Task<List<SalesInvoice>> GetInvoices(string? search = null, int? userId = null);
        Task<SalesInvoice?> GetInvoiceById(long invoiceId, int? userId = null);
        Task<long> CreateSalesInvoice(SalesInvoice invoice);
        Task<List<PaymentMethod>> GetPaymentMethods();
        Task<decimal> GetTodaySalesAmount(int? userId = null);
        Task<List<SalesInvoice>> GetRecentInvoices(int count = 5, int? userId = null);
    }

    public class PaymentMethod
    {
        public int PaymentMethodId { get; set; }
        public string MethodName { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }
}
