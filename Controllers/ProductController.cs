using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

		public async Task<IActionResult> Index(int? categoryId, string? search, int page = 1)
		{
			int pageSize = 9;

			// 1. Lấy toàn bộ query kèm theo Category
			var query = _context.Products.Include(p => p.Category).AsQueryable();

			if (categoryId.HasValue)
				query = query.Where(p => p.CategoryId == categoryId);

			if (!string.IsNullOrWhiteSpace(search))
				query = query.Where(p => p.ProductName.Contains(search));

			// 2. LOGIC QUAN TRỌNG: Gom nhóm theo ProductName để mỗi mẫu chỉ xuất hiện 1 lần
			// Ta lấy ID của sản phẩm đầu tiên trong mỗi nhóm để đại diện
			var groupedQuery = query
				.GroupBy(p => p.ProductName)
				.Select(g => g.First());

			int total = await groupedQuery.CountAsync();
			int totalPages = (int)Math.Ceiling((double)total / pageSize);

			if (page < 1) page = 1;
			if (page > totalPages && totalPages > 0) page = totalPages;

			// 3. Thực hiện phân trang trên danh sách đã gom nhóm
			var products = await groupedQuery
				.Skip((page - 1) * pageSize)
				.Take(pageSize)
				.ToListAsync();

			ViewBag.Categories = await _context.Categories.ToListAsync();
			ViewBag.CurrentCategoryId = categoryId;
			ViewBag.TotalCount = total;
			ViewBag.CurrentSearch = search;
			ViewBag.CurrentPage = page;
			ViewBag.TotalPages = totalPages;
			ViewBag.PageSize = pageSize;

			return View(products);
		}

		[HttpGet]
		public async Task<IActionResult> SearchProducts(string? keyword, int? categoryId)
		{
			var query = _context.Products.AsQueryable();

			if (categoryId.HasValue)
				query = query.Where(p => p.CategoryId == categoryId);

			if (!string.IsNullOrWhiteSpace(keyword))
				query = query.Where(p => p.ProductName.Contains(keyword));

			// Gom nhóm cả trong kết quả tìm kiếm AJAX
			var result = await query
				.GroupBy(p => p.ProductName)
				.Select(g => new {
					id = g.First().Id,
					productName = g.Key,
					price = g.First().Price,
					image = g.First().Image
				})
				.ToListAsync();

			return Json(result);
		}

		public async Task<IActionResult> ProductDetail(int id)
		{
			// 1. Tìm sản phẩm đang click vào
			var product = await _context.Products
				.Include(p => p.Category)
				.FirstOrDefaultAsync(p => p.Id == id);

			if (product == null) return NotFound();

			// 2. Lấy tất cả các size hiện có của CÙNG MỘT TÊN sản phẩm này
			// Điều này giúp khách hàng chọn size ngay trong trang chi tiết
			var allVariants = await _context.Products
				.Where(p => p.ProductName == product.ProductName)
				.Include(p => p.ProductSizes)
					.ThenInclude(ps => ps.Size)
				.SelectMany(p => p.ProductSizes)
				.Select(ps => new {
					SizeName = ps.Size.SizeName,
					Quantity = ps.Quantity,
					// Lưu lại ID của Product cụ thể để khi bấm "Mua" sẽ biết là mua Size nào
					ProductId = ps.ProductId
				})
				.ToListAsync();

			ViewBag.AvailableSizes = allVariants;

			return View(product);
		}
	}
}