using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopQuanAo.Data;
using ShopQuanAo.Models.Entity;
using ShopQuanAo.Services;
using System.Security.Claims;

namespace ShopQuanAo.Controllers
{
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _context; 
        private readonly ProductService _productService;

        public ProductController(ApplicationDbContext context, ProductService productService)
        {
            _context = context;
            _productService = productService;
        }

        public async Task<IActionResult> Index(int? categoryId, string? search, string? price, int? rating, int page = 1)
        {
            int pageSize = 9;
            var result = await _productService.GetPagedProductsAsync(categoryId?.ToString(), search, page, pageSize, price, rating);


            ViewBag.Categories = await _context.Categories.ToListAsync();
            ViewBag.TotalCount = result.TotalCount;
            ViewBag.CurrentSearch = search;
            ViewBag.CurrentPage = result.CurrentPage;
            ViewBag.TotalPages = result.TotalPages;
            ViewBag.PageSize = pageSize;
            ViewBag.CurrentRating = rating;
            ViewBag.CurrentPrice = price;

            if (int.TryParse(categoryId?.ToString(), out int id)) ViewBag.CurrentCategoryId = id;
            else ViewBag.CurrentCategoryName = categoryId;

            return View(result.Products);
        }
        public async Task<IActionResult> Sale(int? categoryId, string? search, string? price, int? rating, int page = 1)
        {
            int pageSize = 9;
            // Điểm mấu chốt: Truyền isSaleOnly = true vào đây
            var result = await _productService.GetPagedProductsAsync(categoryId?.ToString(), search, page, pageSize, price, rating, isSaleOnly: true);

            // Đổ dữ liệu ra ViewBag giống hệt Index để giao diện Sidebar và Phân trang không bị lỗi
            ViewBag.Categories = await _context.Categories.ToListAsync();
            ViewBag.TotalCount = result.TotalCount;
            ViewBag.CurrentSearch = search;
            ViewBag.CurrentPage = result.CurrentPage;
            ViewBag.TotalPages = result.TotalPages;
            ViewBag.PageSize = pageSize;
            ViewBag.CurrentRating = rating;
            ViewBag.CurrentPrice = price;

            if (int.TryParse(categoryId?.ToString(), out int id))
                ViewBag.CurrentCategoryId = id;
            else
                ViewBag.CurrentCategoryName = categoryId;

            // Trả về View Sale.cshtml với danh sách sản phẩm đang giảm giá
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

            // Thêm dòng này để lấy danh sách đánh giá từ DB
            var reviews = await _context.ProductReviews
                                        .Where(r => r.ProductId == id)
                                        .OrderByDescending(r => r.CreatedAt)
                                        .ToListAsync();

            ViewBag.AvailableSizes = sizes;
            ViewBag.Reviews = reviews; // Cực kỳ quan trọng để giao diện không bị lỗi

            return View(product);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitReview(int ProductId, int Rating, string Comment)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login", "Account");

            var review = new ProductReview
            {
                ProductId = ProductId,
                Rating = Rating,
                Comment = Comment,
                UserId = userId,
                FullName = User.Identity.Name ?? "Khách hàng",
                CreatedAt = DateTime.Now
            };

            // Lưu ý: Tên bảng trong DbContext của bạn là ProductReview
            _context.ProductReviews.Add(review);
            await _context.SaveChangesAsync();

            // Sau khi đánh giá xong quay lại trang đơn hàng của khách
            return RedirectToAction("Index", "Customer");
        }
    }
}