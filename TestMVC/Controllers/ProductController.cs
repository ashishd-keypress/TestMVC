using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using TestMVC.Interface;
using TestMVC.Models;

namespace TestMVC.Controllers
{
    public class ProductController : Controller
    {
        private const int PageSize = 10;
        private readonly IProductRepository _repo;

        public ProductController(IProductRepository repo)
        {
            _repo = repo;
        }

        // GET: /Product?search=abc&pageNo=1
        public async Task<IActionResult> Index(string? search, int pageNo = 1)
        {
            if (pageNo < 1) pageNo = 1;

            var products = await _repo.GetProductsAsync(pageNo, PageSize, search);

            ViewBag.Search = search;
            ViewBag.PageNo = pageNo;
            ViewBag.HasNext = products.Count == PageSize;   // full page means there may be more

            return View(products);
        }

        // GET: /Product/Create
        [HttpGet]
        public IActionResult Create()
        {
            return View(new Product());
        }

        // POST: /Product/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product)
        {
            try
            {
                await _repo.AddProductAsync(product);
                TempData["Success"] = "Product added successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (SqlException ex)
            {
                // Validation messages raised inside the stored procedure show here
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(product);
            }
        }

    }
}
