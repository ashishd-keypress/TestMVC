using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
            var invoices = await _salesService.GetInvoices(search);
            ViewBag.Search = search;
            ViewBag.TodaySales = await _salesService.GetTodaySalesAmount();
            return View(invoices);
        }

        // GET: /Sales/Create (POS Billing Screen)
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await LoadDropdownsAsync();
            var model = new SalesInvoice
            {
                InvoiceNumber = $"INV-{DateTime.Now:yyyyMMdd}-{new Random().Next(1000, 9999)}",
                InvoiceDate = DateTime.Now
            };
            return View(model);
        }

        // POST: /Sales/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SalesInvoice invoice, string? itemsJson)
        {
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
                await LoadDropdownsAsync(invoice.CustomerId, invoice.PaymentMethodId);
                return View(invoice);
            }

            try
            {
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

            await LoadDropdownsAsync(invoice.CustomerId, invoice.PaymentMethodId);
            return View(invoice);
        }

        // GET: /Sales/Invoice/5 (Printable Bill)
        [HttpGet]
        public async Task<IActionResult> Invoice(long id)
        {
            var invoice = await _salesService.GetInvoiceById(id);
            if (invoice == null)
            {
                TempData["Error"] = "Invoice not found.";
                return RedirectToAction(nameof(Index));
            }

            return View(invoice);
        }

        private async Task LoadDropdownsAsync(long? selectedCustomer = null, int? selectedPaymentMethod = null)
        {
            var customers = await _customerService.GetCustomers();
            var paymentMethods = await _salesService.GetPaymentMethods();
            var (products, _, _) = await _productService.GetProduct(new Models.ReqRes.GetProductReq { PageSize = 1000, IsActive = true });

            ViewBag.Customers = new SelectList(customers, "CustomerId", "CustomerName", selectedCustomer);
            ViewBag.PaymentMethods = new SelectList(paymentMethods, "PaymentMethodId", "MethodName", selectedPaymentMethod ?? 1);
            ViewBag.ProductsList = products;
            ViewBag.CustomersList = customers;
        }
    }
}
