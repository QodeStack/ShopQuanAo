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

		// SỬA VỊ TRÍ 1: Đổi IdentityUser thành ApplicationUser
		private readonly UserManager<ApplicationUser> _userManager;

		// SỬA VỊ TRÍ 2: Đổi tham số UserManager trong Constructor thành ApplicationUser
		public ProductController(ApplicationDbContext context, ProductService productService, UserManager<ApplicationUser> userManager)
		{
			_context = context;
			_productService = productService;
			_userManager = userManager;
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
		public async Task<IActionResult> PostReview(ProductReview review)
		{
			var userId = _userManager.GetUserId(User);

			// SỬA VỊ TRÍ 3: Chỗ này sẽ tự động hiểu user là ApplicationUser
			var user = await _userManager.FindByIdAsync(userId);

			review.UserId = userId;

			// Nếu ApplicationUser của bạn có trường FullName thì dùng user.FullName
			// Nếu không thì dùng UserName như cũ
			review.FullName = user?.UserName ?? "Khách hàng";
			review.CreatedAt = DateTime.Now;

			if (ModelState.IsValid)
			{
				_context.ProductReviews.Add(review);
				await _context.SaveChangesAsync();
				return RedirectToAction("ProductDetail", new { id = review.ProductId });
			}

			return RedirectToAction("ProductDetail", new { id = review.ProductId });
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

			_context.ProductReviews.Add(review);
			await _context.SaveChangesAsync();

			return RedirectToAction("Index", "Customer");
		}
	}
}