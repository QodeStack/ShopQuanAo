using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopQuanAo.Data;
using ShopQuanAo.Models.BEAN.Entity;
using ShopQuanAo.BO;
using System.Text.RegularExpressions;
using System.Text;

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

        public async Task<IActionResult> Index(int? categoryId, string? categoryName, string? search, string? price, int? rating, string? sort, int page = 1)
        {
            int pageSize = 12;

            // 1. Xử lý lấy CategoryId từ Name nếu cần
            if (!string.IsNullOrEmpty(categoryName) && categoryId == null)
            {
                var cat = await _context.Categories.FirstOrDefaultAsync(c => c.CategoryName == categoryName);
                if (cat != null) categoryId = cat.Id;
            }

            // 2. Gọi Service với đầy đủ tham số lọc và SẮP XẾP (sort)
            var result = await _productService.GetPagedProductsAsync(
                categoryId?.ToString(),
                search,
                page,
                pageSize,
                price,
                rating,
                isSaleOnly: false,
                sort: sort);

            // 3. Logic xử lý Banner (Giữ nguyên logic của bạn)
            string bannerFileName = "Banner_tatcasanpham.jpg";
            string currentTitle = "TẤT CẢ SẢN PHẨM";

            if (categoryId.HasValue)
            {
                var currentCat = await _context.Categories.FindAsync(categoryId);
                if (currentCat != null)
                {
                    currentTitle = currentCat.CategoryName.ToUpper();
                    string slug = GenerateSlug(currentCat.CategoryName);
                    bannerFileName = $"Banner_{slug}.jpg";
                }
            }

            // 4. Lấy tất cả ratings cho các sản phẩm hiển thị
            var allReviews = new Dictionary<int, List<ProductReview>>();
            if (result.Products.Any())
            {
                var productIds = result.Products.Select(p => p.Id).ToList();
                var reviews = await _context.ProductReviews
                    .Where(r => productIds.Contains(r.ProductId))
                    .ToListAsync();

                foreach (var productId in productIds)
                {
                    allReviews[productId] = reviews.Where(r => r.ProductId == productId).ToList();
                }
            }

            // 5. Thiết lập ViewBag để hiển thị giao diện
            ViewBag.BannerPath = $"/Image/Banner_sanpham/{bannerFileName}";
            ViewBag.BannerTitle = currentTitle;
            ViewBag.CurrentSort = sort; // Quan trọng: Để Select Box không bị reset
            ViewBag.AllReviews = allReviews; // Thêm reviews cho từng sản phẩm

            await SetProductViewBagData(result, categoryId, search, rating, price, pageSize);

            // 6. Trả về View với sản phẩm
            return View(result.Products);
        }

        public async Task<IActionResult> Sale(int? categoryId, string? search, string? price, int? rating, string? sort, int page = 1)
        {
            int pageSize = 12;
            // 1. Gọi service lấy sản phẩm Sale
            var result = await _productService.GetPagedProductsAsync(
                categoryId?.ToString(),
                search,
                page,
                pageSize,
                price,
                rating,
                isSaleOnly: true,
                sort: sort);

            // 2. BỔ SUNG: Lấy tất cả ratings cho các sản phẩm Sale đang hiển thị
            var allReviews = new Dictionary<int, List<ProductReview>>();
            if (result.Products != null)
            {
                var productIds = ((IEnumerable<Product>)result.Products).Select(p => p.Id).ToList();
                var reviews = await _context.ProductReviews
                    .Where(r => productIds.Contains(r.ProductId))
                    .ToListAsync();

                foreach (var productId in productIds)
                {
                    allReviews[productId] = reviews.Where(r => r.ProductId == productId).ToList();
                }
            }

            // 3. Thiết lập ViewBag
            ViewBag.CurrentSort = sort;
            ViewBag.AllReviews = allReviews; // Đưa dữ liệu đánh giá ra View trang Sale

            await SetProductViewBagData(result, categoryId, search, rating, price, pageSize);

            // Trả về View Sale
            return View(result.Products);
        }

        private string GenerateSlug(string phrase)
        {
            if (string.IsNullOrEmpty(phrase)) return "";
            string str = phrase.ToLower();
            str = Regex.Replace(str, @"[áàảãạâấầẩẫậăắằẳẵặ]", "a");
            str = Regex.Replace(str, @"[éèẻẽẹêếềểễệ]", "e");
            str = Regex.Replace(str, @"[íìỉĩị]", "i");
            str = Regex.Replace(str, @"[óòỏõọôốồổỗộơớờởỡợ]", "o");
            str = Regex.Replace(str, @"[úùủũụưứừửữự]", "u");
            str = Regex.Replace(str, @"[ýỳỷỹỵ]", "y");
            str = str.Replace("đ", "d").Replace(" ", "");
            return str;
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

			var reviews = await _context.ProductReviews
										.Where(r => r.ProductId == id)
										.OrderByDescending(r => r.CreatedAt)
										.ToListAsync();

			// LẤY VOUCHER TỪ DATABASE
			// Lưu ý: Hãy kiểm tra chính xác tên cột trong bảng Vouchers của bạn là gì (ExpiryDate hay ExpirationDate...)
			// Lấy danh sách Voucher đang hoạt động từ bảng Vouchers
			var coupons = await _context.Vouchers
				.Where(v => v.IsActive == true && v.Quantity > 0 && v.IsPublic == true)
				.OrderBy(v => v.MinOrderAmount) // Đổi từ MinOrderValue thành MinOrderAmount theo DB
				.ToListAsync();

			ViewBag.AvailableSizes = sizes;
			ViewBag.Reviews = reviews;
			ViewBag.Coupons = coupons; // Gửi danh sách này sang View

			return View(product);
		}

		[HttpPost]
        [Authorize]
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
            return RedirectToAction("ProductDetail", new { id = ProductId });
        }

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
            ViewBag.CurrentCategoryId = categoryId;
        }
    }
}