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

		// Đã sửa: Chuyển tham số đầu tiên thành string để nhận được cả ID số và Chuỗi danh mục
		public async Task<IActionResult> Index(string? category, string? search, int page = 1)
		{
			int pageSize = 9;

			// 1. Lấy toàn bộ query kèm theo Category
			var query = _context.Products.Include(p => p.Category).AsQueryable();

			// 2. LOGIC LỌC DANH MỤC (Sửa lỗi logic tại đây)
			if (!string.IsNullOrWhiteSpace(category))
			{
				// Nếu category là số (dành cho menu bên trái cũ)
				if (int.TryParse(category, out int id))
				{
					query = query.Where(p => p.CategoryId == id);
					ViewBag.CurrentCategoryId = id;
				}
				else
				{
					// Nếu category là chuỗi (dành cho Mega Menu mới)
					// So khớp với CategoryName trong Database (loại bỏ khoảng trắng để dễ khớp hơn)
					query = query.Where(p => p.Category.CategoryName.Replace(" ", "").Contains(category));
					ViewBag.CurrentCategoryName = category;
				}
			}

			// 3. Lọc theo tìm kiếm
			if (!string.IsNullOrWhiteSpace(search))
				query = query.Where(p => p.ProductName.Contains(search));

			// 4. Gom nhóm theo ProductName để mỗi mẫu chỉ xuất hiện 1 lần
			var groupedQuery = query
				.GroupBy(p => p.ProductName)
				.Select(g => g.First());

			int total = await groupedQuery.CountAsync();
			int totalPages = (int)Math.Ceiling((double)total / pageSize);

			if (page < 1) page = 1;
			if (page > totalPages && totalPages > 0) page = totalPages;

			// 5. Thực hiện phân trang trên danh sách đã gom nhóm
			var products = await groupedQuery
				.Skip((page - 1) * pageSize)
				.Take(pageSize)
				.ToListAsync();

			ViewBag.Categories = await _context.Categories.ToListAsync();
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
			var product = await _context.Products
				.Include(p => p.Category)
				.FirstOrDefaultAsync(p => p.Id == id);

			if (product == null) return NotFound();

			var allVariants = await _context.Products
				.Where(p => p.ProductName == product.ProductName)
				.Include(p => p.ProductSizes)
					.ThenInclude(ps => ps.Size)
				.SelectMany(p => p.ProductSizes)
				.Select(ps => new {
					SizeName = ps.Size.SizeName,
					Quantity = ps.Quantity,
					ProductId = ps.ProductId
				})
				.ToListAsync();

			ViewBag.AvailableSizes = allVariants;

			return View(product);
		}
	}
}