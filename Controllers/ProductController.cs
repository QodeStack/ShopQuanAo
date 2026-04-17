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

			if (!string.IsNullOrEmpty(categoryName) && categoryId == null)
			{
				var cat = await _context.Categories.FirstOrDefaultAsync(c => c.CategoryName == categoryName);
				if (cat != null) categoryId = cat.Id;
			}

			var result = await _productService.GetPagedProductsAsync(categoryId?.ToString(), search, page, pageSize, price, rating);

			// --- LOGIC BANNER MỚI: TRỎ VÀO Banner_sanpham ---
			string bannerFileName = "Banner_tatcasanpham.jpg";
			string currentTitle = "TẤT CẢ SẢN PHẨM";

			if (categoryId.HasValue)
			{
				var currentCat = await _context.Categories.FindAsync(categoryId);
				if (currentCat != null)
				{
					currentTitle = currentCat.CategoryName.ToUpper();
					// Chuyển "Áo Thun" -> "aothun"
					string slug = GenerateSlug(currentCat.CategoryName);
					// Ghép thành "Banner_aothun.jpg" khớp với ảnh trong thư mục Banner_sanpham
					bannerFileName = $"Banner_{slug}.jpg";
				}
			}

			ViewBag.BannerPath = $"/Image/Banner_sanpham/{bannerFileName}";
			ViewBag.BannerTitle = currentTitle;
			ViewBag.CurrentSort = sort;

			await SetProductViewBagData(result, categoryId, search, rating, price, pageSize);

			// Trả về view kèm sản phẩm
			return View(result.Products);
		}

		public async Task<IActionResult> Sale(int? categoryId, string? search, string? price, int? rating, string? sort, int page = 1)
		{
			int pageSize = 12;
			var result = await _productService.GetPagedProductsAsync(categoryId?.ToString(), search, page, pageSize, price, rating, isSaleOnly: true);

			// Không dùng ViewBag.BannerPath ở đây để tránh ảnh hưởng giao diện Index
			ViewBag.CurrentSort = sort;

			await SetProductViewBagData(result, categoryId, search, rating, price, pageSize);

			// TRẢ VỀ ĐÚNG VIEW SALE CỦA NÓ (Sidebar bên trái)
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

			ViewBag.AvailableSizes = sizes;
			ViewBag.Reviews = reviews;

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