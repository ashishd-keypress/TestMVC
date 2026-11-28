using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using TestMVC.Interface;
using TestMVC.Models;

namespace TestMVC.Controllers
{
    [Authorize]
    public class SalesController : Controller
    {
        private readonly ISalesService _salesService;
        private readonly ICustomerService _customerService;
        private readonly IProductService _productService;
        private readonly IDeviceUnitService _deviceUnitService;

        public SalesController(
            ISalesService salesService,
            ICustomerService customerService,
            IProductService productService,
            IDeviceUnitService deviceUnitService)
        {
            _salesService = salesService;
            _customerService = customerService;
            _productService = productService;
            _deviceUnitService = deviceUnitService;
        }

        // GET: /Sales
        [HttpGet]
        public async Task<IActionResult> Index(string? search)
        {
            int? currentUserId = Utility.Helper.GetUserId(User);
            var invoices = await _salesService.GetInvoices(search, currentUserId);
            ViewBag.Search = search;
            ViewBag.TodaySales = await _salesService.GetTodaySalesAmount(currentUserId);
            return View(invoices);
        }

        // GET: /Sales/Create (POS Billing Screen)
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            int? currentUserId = Utility.Helper.GetUserId(User);
            var defaultPayId = await LoadDropdownsAsync(currentUserId: currentUserId);
            var model = new SalesInvoice
            {
                InvoiceNumber = $"INV-{DateTime.Now:yyyyMMdd}-{new Random().Next(1000, 9999)}",
                InvoiceDate = DateTime.Now,
                PaymentMethodId = defaultPayId ?? 0
            };
            return View(model);
        }

        // POST: /Sales/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SalesInvoice invoice, string? itemsJson)
        {
            int? currentUserId = Utility.Helper.GetUserId(User);

            if (!string.IsNullOrWhiteSpace(itemsJson))
            {
                try
                {
                    invoice.Items = JsonConvert.DeserializeObject<List<SalesInvoiceItem>>(itemsJson) ?? new List<SalesInvoiceItem>();
                }
                catch
                {
                    ModelState.AddModelError(string.Empty, "Invalid items format.");
                }
            }

            if (invoice.Items == null || invoice.Items.Count == 0)
            {
                ModelState.AddModelError(string.Empty, "Please add at least one product to the invoice bill.");
            }

            if (!ModelState.IsValid)
            {
                await LoadDropdownsAsync(invoice.CustomerId, invoice.PaymentMethodId, currentUserId);
                return View(invoice);
            }

            try
            {
                invoice.UserId = currentUserId;
                invoice.CreatedBy = currentUserId;

                long id = await _salesService.CreateSalesInvoice(invoice);
                if (id > 0)
                {
                    TempData["Success"] = $"Invoice #{invoice.InvoiceNumber} generated successfully!";
                    return RedirectToAction(nameof(Invoice), new { id });
                }

                ModelState.AddModelError(string.Empty, "Unable to create sales invoice.");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"Error generating invoice: {ex.Message}");
            }

            await LoadDropdownsAsync(invoice.CustomerId, invoice.PaymentMethodId, currentUserId);
            return View(invoice);
        }

        // GET: /Sales/Invoice/5 (Printable Bill)
        [HttpGet]
        public async Task<IActionResult> Invoice(long id)
        {
            int? currentUserId = Utility.Helper.GetUserId(User);
            var invoice = await _salesService.GetInvoiceById(id, currentUserId);
            if (invoice == null)
            {
                TempData["Error"] = "Invoice not found.";
                return RedirectToAction(nameof(Index));
            }

            return View(invoice);
        }

        private async Task<int?> LoadDropdownsAsync(long? selectedCustomer = null, int? selectedPaymentMethod = null, int? currentUserId = null)
        {
            currentUserId ??= Utility.Helper.GetUserId(User);
            var customers = await _customerService.GetCustomers(userId: currentUserId);
            var paymentMethods = await _salesService.GetPaymentMethods(currentUserId);
            var (products, _, _) = await _productService.GetProduct(new Models.ReqRes.GetProductReq { PageSize = 1000, IsActive = true, UserId = currentUserId });
            int? defaultPayId = selectedPaymentMethod ?? paymentMethods.FirstOrDefault()?.PaymentMethodId;

            ViewBag.Customers = new SelectList(customers, "CustomerId", "CustomerName", selectedCustomer);
            ViewBag.PaymentMethods = new SelectList(paymentMethods, "PaymentMethodId", "MethodName", defaultPayId);
            ViewBag.ProductsList = products;
            ViewBag.CustomersList = customers;
            return defaultPayId;
        }
    }
}
