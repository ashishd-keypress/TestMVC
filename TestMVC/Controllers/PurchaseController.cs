using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using TestMVC.Interface;
using TestMVC.Models;

namespace TestMVC.Controllers
{
    [Authorize]
    public class PurchaseController : Controller
    {
        private readonly IPurchaseService _purchaseService;
        private readonly ISupplierService _supplierService;
        private readonly IProductService _productService;

        public PurchaseController(
            IPurchaseService purchaseService,
            ISupplierService supplierService,
            IProductService productService)
        {
            _purchaseService = purchaseService;
            _supplierService = supplierService;
            _productService = productService;
        }

        // GET: /Purchase
        [HttpGet]
        public async Task<IActionResult> Index(string? search)
        {
            int? currentUserId = Utility.Helper.GetUserId(User);
            var purchases = await _purchaseService.GetPurchases(search, currentUserId);
            ViewBag.Search = search;
            return View(purchases);
        }

        // GET: /Purchase/Create
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            int? currentUserId = Utility.Helper.GetUserId(User);
            await LoadDropdownsAsync(currentUserId: currentUserId);
            var model = new Purchase
            {
                InvoiceNumber = $"PUR-{DateTime.Now:yyyyMMdd}-{new Random().Next(100, 999)}",
                PurchaseDate = DateTime.Today
            };
            return View(model);
        }

        // POST: /Purchase/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Purchase purchase, string? itemsJson)
        {
            int? currentUserId = Utility.Helper.GetUserId(User);
            purchase.UserId = currentUserId;
            purchase.CreatedBy = currentUserId;

            if (!string.IsNullOrWhiteSpace(itemsJson))
            {
                try
                {
                    purchase.Items = JsonConvert.DeserializeObject<List<PurchaseItem>>(itemsJson) ?? new List<PurchaseItem>();
                }
                catch
                {
                    ModelState.AddModelError(string.Empty, "Invalid items format.");
                }
            }

            if (purchase.Items == null || purchase.Items.Count == 0)
            {
                ModelState.AddModelError(string.Empty, "Please add at least one product item to the purchase.");
            }

            if (!ModelState.IsValid)
            {
                await LoadDropdownsAsync(purchase.SupplierId, currentUserId);
                return View(purchase);
            }

            try
            {
                long id = await _purchaseService.SavePurchase(purchase);
                if (id > 0)
                {
                    TempData["Success"] = $"Purchase invoice #{purchase.InvoiceNumber} saved and inventory updated successfully!";
                    return RedirectToAction(nameof(Details), new { id });
                }

                ModelState.AddModelError(string.Empty, "Unable to save purchase.");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"Error saving purchase: {ex.Message}");
            }

            await LoadDropdownsAsync(purchase.SupplierId, currentUserId);
            return View(purchase);
        }

        // GET: /Purchase/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(long id)
        {
            int? currentUserId = Utility.Helper.GetUserId(User);
            var purchase = await _purchaseService.GetPurchaseById(id, currentUserId);
            if (purchase == null)
            {
                TempData["Error"] = "Purchase record not found.";
                return RedirectToAction(nameof(Index));
            }

            return View(purchase);
        }

        private async Task LoadDropdownsAsync(long? selectedSupplier = null, int? currentUserId = null)
        {
            var suppliers = await _supplierService.GetSuppliers(userId: currentUserId);
            var (products, _, _) = await _productService.GetProduct(new Models.ReqRes.GetProductReq { PageSize = 1000, IsActive = true, UserId = currentUserId });

            ViewBag.Suppliers = new SelectList(suppliers, "SupplierId", "SupplierName", selectedSupplier);
            ViewBag.ProductsList = products;
        }
    }
}
