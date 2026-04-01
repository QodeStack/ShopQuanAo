using Microsoft.AspNetCore.Mvc;
using ShopQuanAo.Data;
using ShopQuanAo.Models;

namespace ShopQuanAo.Controllers
{
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProductController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index(int? categoryId, string? search, int page = 1)
        {
            int pageSize = 9;

            var query = _context.Products.AsQueryable();

            if (categoryId.HasValue)
                query = query.Where(p => p.CategoryId == categoryId);

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(p => p.ProductName.Contains(search));

            int total = query.Count();
            var products = query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.Categories = _context.Categories.ToList();
            ViewBag.CurrentCategoryId = categoryId;
            ViewBag.TotalCount = total;
            ViewBag.CurrentSearch = search;

            return View(products);
        }

        // Endpoint riêng cho AJAX search
        [HttpGet]
        public IActionResult SearchProducts(string? keyword, int? categoryId)
        {
            var query = _context.Products.AsQueryable();

            if (categoryId.HasValue)
                query = query.Where(p => p.CategoryId == categoryId);

            if (!string.IsNullOrWhiteSpace(keyword))
                query = query.Where(p => p.ProductName.Contains(keyword));

            var result = query.Select(p => new {
                p.Id,
                p.ProductName,
                p.Price,
                p.Image
            }).ToList();

            return Json(result);
        }

        public IActionResult ProductDetail(int id)
        {
            var product = _context.Products.FirstOrDefault(p => p.Id == id);
            if (product == null) return NotFound();
            return View(product);
        }
    }
}