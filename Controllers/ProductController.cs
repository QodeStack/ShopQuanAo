using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopQuanAo.Data;
using ShopQuanAo.Models.Entity;
using ShopQuanAo.Services;

namespace ShopQuanAo.Controllers
{
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ProductService _productService;
        private readonly UserManager<ApplicationUser> _userManager;

        public ProductController(ApplicationDbContext context,
                                 ProductService productService,
                                 UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _productService = productService;
            _userManager = userManager;
        }

        // 1. Trang danh sách sản phẩm (Có phân trang, lọc, tìm kiếm)
        public async Task<IActionResult> Index(int? categoryId, string? search, string? price, int? rating, int page = 1)
        {
            int pageSize = 9;
            var result = await _productService.GetPagedProductsAsync(categoryId?.ToString(), search, page, pageSize, price, rating);

            await SetProductViewBagData(result, categoryId, search, rating, price, pageSize);

            return View(result.Products);
        }

        // 2. Trang sản phẩm đang giảm giá
        public async Task<IActionResult> Sale(int? categoryId, string? search, string? price, int? rating, int page = 1)
        {
            int pageSize = 9;
            // Filter isSaleOnly = true để chỉ lấy sản phẩm có SalePrice > 0
            var result = await _productService.GetPagedProductsAsync(categoryId?.ToString(), search, page, pageSize, price, rating, isSaleOnly: true);

            await SetProductViewBagData(result, categoryId, search, rating, price, pageSize);

            return View(result.Products);
        }

        // 3. API Tìm kiếm nhanh (Trả về JSON cho chức năng gợi ý search)
        [HttpGet]
        public async Task<IActionResult> SearchProducts(string? keyword, int? categoryId)
        {
            var result = await _productService.SearchQuickAsync(keyword, categoryId);
            return Json(result);
        }

        // 4. Chi tiết sản phẩm
        public async Task<IActionResult> ProductDetail(int id)
        {
            var (product, sizes) = await _productService.GetProductDetailAsync(id);
            if (product == null) return NotFound();

            // Lấy danh sách đánh giá từ database cho sản phẩm này
            var reviews = await _context.ProductReviews
                                        .Where(r => r.ProductId == id)
                                        .OrderByDescending(r => r.CreatedAt)
                                        .ToListAsync();

            ViewBag.AvailableSizes = sizes;
            ViewBag.Reviews = reviews;

            return View(product);
        }

        // 5. Gửi đánh giá (Dùng cho cả Form ở trang chi tiết hoặc trang quản lý khách hàng)
        [HttpPost]
        [Authorize] // Bắt buộc đăng nhập mới được đánh giá
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitReview(int ProductId, int Rating, string Comment, string ReturnUrl = "ProductDetail")
        {
            var userId = _userManager.GetUserId(User);
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null) return RedirectToAction("Login", "Account");

            var review = new ProductReview
            {
                ProductId = ProductId,
                Rating = Rating,
                Comment = Comment,
                UserId = userId,
                FullName = User.Identity.Name ?? "Khách hàng",
                CreatedAt = DateTime.Now
            };

            _context.ProductReviews.Add(review);
            await _context.SaveChangesAsync();

            // Nếu ReturnUrl là Customer thì về trang cá nhân, ngược lại về trang chi tiết sản phẩm vừa đánh giá
            if (ReturnUrl == "Customer")
            {
                return RedirectToAction("Index", "Customer");
            }

            return RedirectToAction("ProductDetail", new { id = ProductId });
        }

        // --- Hàm phụ trợ (Private helper) giúp làm gọn code ---
        private async Task SetProductViewBagData(dynamic result, int? categoryId, string? search, int? rating, string? price, int pageSize)
        {
            ViewBag.Categories = await _context.Categories.ToListAsync();
            ViewBag.TotalCount = result.TotalCount;
            ViewBag.CurrentSearch = search;
            ViewBag.CurrentPage = result.CurrentPage;
            ViewBag.TotalPages = result.TotalPages;
            ViewBag.PageSize = pageSize;
            ViewBag.CurrentRating = rating;
            ViewBag.CurrentPrice = price;

            // Xử lý logic Category ID để hiển thị active trên Sidebar/Menu
            ViewBag.CurrentCategoryId = categoryId;
        }
    }
}