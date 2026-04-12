using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopQuanAo.Data;
using ShopQuanAo.Services;

namespace ShopQuanAo.Controllers
{
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _context; // Vẫn cần để lấy list Category nhanh
        private readonly ProductService _productService;

        public ProductController(ApplicationDbContext context, ProductService productService)
        {
            _context = context;
            _productService = productService;
        }

        public async Task<IActionResult> Index(string? category, string? search, int page = 1)
        {
            int pageSize = 9;
            var result = await _productService.GetPagedProductsAsync(category, search, page, pageSize);

            ViewBag.Categories = await _context.Categories.ToListAsync();
            ViewBag.TotalCount = result.TotalCount;
            ViewBag.CurrentSearch = search;
            ViewBag.CurrentPage = result.CurrentPage;
            ViewBag.TotalPages = result.TotalPages;
            ViewBag.PageSize = pageSize;

            // Truyền thông tin Category để UI hiển thị active menu
            if (int.TryParse(category, out int id)) ViewBag.CurrentCategoryId = id;
            else ViewBag.CurrentCategoryName = category;

            return View(result.Products);
        }

        [HttpGet]
        public async Task<IActionResult> SearchProducts(string? keyword, int? categoryId)
        {
            var result = await _productService.SearchQuickAsync(keyword, categoryId);
            return Json(result);
        }

        public async Task<IActionResult> ProductDetail(int id)
        {
            var (product, sizes) = await _productService.GetProductDetailAsync(id);
            if (product == null) return NotFound();

            ViewBag.AvailableSizes = sizes;
            return View(product);
        }
    }
}