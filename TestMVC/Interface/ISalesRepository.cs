using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using TestMVC.Models;

namespace TestMVC.Interface
{
    public interface ISalesRepository
    {
        Task<List<SalesInvoice>> GetInvoices(string? search = null, int? userId = null);
        Task<SalesInvoice?> GetInvoiceById(long invoiceId, int? userId = null);
        Task<long> CreateSalesInvoice(SalesInvoice invoice);
        Task<List<PaymentMethod>> GetPaymentMethods(int? userId = null);
        Task<PaymentMethod?> GetPaymentMethodById(int paymentMethodId, int? userId = null);
        Task<(bool Success, string Message, PaymentMethod? PaymentMethod)> SavePaymentMethod(PaymentMethod paymentMethod);
        Task<(bool Success, string Message)> DeletePaymentMethod(int paymentMethodId, int? userId = null);
        Task<decimal> GetTodaySalesAmount(int? userId = null);
        Task<List<SalesInvoice>> GetRecentInvoices(int count = 5, int? userId = null);
    }

    public interface ISalesService
    {
        Task<List<SalesInvoice>> GetInvoices(string? search = null, int? userId = null);
        Task<SalesInvoice?> GetInvoiceById(long invoiceId, int? userId = null);
        Task<long> CreateSalesInvoice(SalesInvoice invoice);
        Task<List<PaymentMethod>> GetPaymentMethods(int? userId = null);
        Task<PaymentMethod?> GetPaymentMethodById(int paymentMethodId, int? userId = null);
        Task<(bool Success, string Message, PaymentMethod? PaymentMethod)> SavePaymentMethod(PaymentMethod paymentMethod);
        Task<(bool Success, string Message)> DeletePaymentMethod(int paymentMethodId, int? userId = null);
        Task<decimal> GetTodaySalesAmount(int? userId = null);
        Task<List<SalesInvoice>> GetRecentInvoices(int count = 5, int? userId = null);
    }

    public class PaymentMethod
    {
        public int PaymentMethodId { get; set; }

        [Required(ErrorMessage = "Payment Mode name is required.")]
        [Display(Name = "Payment Mode")]
        [StringLength(100, ErrorMessage = "Payment Mode name cannot exceed 100 characters.")]
        public string MethodName { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public int? UserId { get; set; }
    }
}
