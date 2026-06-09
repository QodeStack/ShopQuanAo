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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
            int pageSize = 20;

            // 1. Xử lý lấy CategoryId từ Name nếu cần
            if (!string.IsNullOrEmpty(categoryName) && categoryId == null)
            {
                var cat = await _context.Categories.FirstOrDefaultAsync(c => c.CategoryName == categoryName);
                if (cat != null) categoryId = cat.Id;
            }

            // 2. Gọi Service với đầy đủ tham số lọc và Sắp xếp
            var result = await _productService.GetPagedProductsAsync(
                categoryId?.ToString(),
                search,
                page,
                pageSize,
                price,
                rating,
                isSaleOnly: false,
                sort: sort);

            // 3. Logic xử lý Banner 
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

            // 4. Lấy tất cả ratings cho các sản phẩm hiển thị thông qua Service
            var allReviews = new Dictionary<int, List<ProductReview>>();
            if (result.Products != null && result.Products.Any())
            {
                var productIds = result.Products.Select(p => p.Id).ToList();
                allReviews = await _productService.GetReviewsForProductsAsync(productIds);
            }

            // 5. Thiết lập ViewBag để hiển thị giao diện
            ViewBag.BannerPath = $"/Image/Banner_sanpham/{bannerFileName}";
            ViewBag.BannerTitle = currentTitle;
            ViewBag.CurrentSort = sort;
            ViewBag.AllReviews = allReviews;

            await SetProductViewBagData(result, categoryId, search, rating, price, pageSize);

            return View(result.Products);
        }

        public async Task<IActionResult> Sale(int? categoryId, string? search, string? price, int? rating, string? sort, int page = 1)
        {
            int pageSize = 20;

            // 1. Gọi Service lấy sản phẩm Sale
            var result = await _productService.GetPagedProductsAsync(
                categoryId?.ToString(),
                search,
                page,
                pageSize,
                price,
                rating,
                isSaleOnly: true,
                sort: sort);

            // 2. Lấy tất cả ratings thông qua Service
            var allReviews = new Dictionary<int, List<ProductReview>>();
            if (result.Products != null && result.Products.Any())
            {
                var productIds = result.Products.Select(p => p.Id).ToList();
                allReviews = await _productService.GetReviewsForProductsAsync(productIds);
            }

            ViewBag.CurrentSort = sort;
            ViewBag.AllReviews = allReviews;

            await SetProductViewBagData(result, categoryId, search, rating, price, pageSize);

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
            // Controller -> Service hoàn toàn sạch sẽ
            var detailDto = await _productService.GetProductDetailDisplayAsync(id);
            if (detailDto == null) return NotFound();

            // Nạp dữ liệu vào ViewBag để đẩy ra giao diện
            ViewBag.AvailableSizes = detailDto.AvailableSizes;
            ViewBag.Reviews = detailDto.Reviews;
            ViewBag.Coupons = detailDto.Coupons;
            ViewBag.RelatedProducts = detailDto.RelatedProducts;

            return View(detailDto.Product);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitReview(int ProductId, int OrderId, int Rating, string Comment)
        {
            var userId = _userManager.GetUserId(User);

            // Đảm bảo userId không null (Fix CS8604)
            if (string.IsNullOrEmpty(userId)) return RedirectToAction("Login", "Account");

            // Đảm bảo Identity an toàn (Fix CS8602)
            var userName = User?.Identity?.Name ?? "Khách hàng";

            // Controller chuyển tiếp dữ liệu xuống Service xử lý
            await _productService.CreateProductReviewAsync(ProductId, OrderId, Rating, Comment, userId, userName);

            return RedirectToAction("ProductDetail", new { id = ProductId });
        }

        private async Task SetProductViewBagData(dynamic result, int? categoryId, string? search, int? rating, string? price, int pageSize)
        {
            // Vẫn giữ lại _context ở đây để trả về List<Category> cho View mà không làm hỏng giao diện cũ của bạn
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
    }
}