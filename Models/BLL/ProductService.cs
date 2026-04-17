using Microsoft.EntityFrameworkCore;
using ShopQuanAo.Data;
using ShopQuanAo.Models.DTO;
using ShopQuanAo.Models.Entity;

namespace ShopQuanAo.Services
{
	public class ProductService
	{
		private readonly ApplicationDbContext _context;

		public ProductService(ApplicationDbContext context)
		{
			_context = context;
		}

		public async Task<ProductPagedDto> GetPagedProductsAsync(
			string? category,
			string? search,
			int page,
			int pageSize,
			string? price = null,
			int? rating = null,
			bool isSaleOnly = false,
			string? sort = null)
		{
			// 1. Khởi tạo Query cơ bản
			var query = _context.Products
				.Include(p => p.Category)
				.Include(p => p.ProductReviews)
				.Include(p => p.ProductSizes)
				.AsQueryable();

			// 2. Logic Sale
			if (isSaleOnly)
			{
				query = query.Where(p => p.SalePrice > 0);
			}

			// 3. Lọc theo danh mục
			if (!string.IsNullOrWhiteSpace(category))
			{
				if (int.TryParse(category, out int id))
					query = query.Where(p => p.CategoryId == id);
				else
					query = query.Where(p => p.Category.CategoryName.Contains(category));
			}

			// 4. Lọc theo từ khóa
			if (!string.IsNullOrWhiteSpace(search))
				query = query.Where(p => p.ProductName.Contains(search) || p.BrandName.Contains(search));

			// 5. Lọc theo khoảng giá (Tính trên giá thực tế đang bán)
			query = price switch
			{
				"under500" => query.Where(p => (p.SalePrice > 0 ? p.SalePrice : p.Price) < 500000),
				"500to1000" => query.Where(p => (p.SalePrice > 0 ? p.SalePrice : p.Price) >= 500000 && (p.SalePrice > 0 ? p.SalePrice : p.Price) <= 1000000),
				"1000to2000" => query.Where(p => (p.SalePrice > 0 ? p.SalePrice : p.Price) > 1000000 && (p.SalePrice > 0 ? p.SalePrice : p.Price) <= 2000000),
				"over2000" => query.Where(p => (p.SalePrice > 0 ? p.SalePrice : p.Price) > 2000000),
				_ => query
			};

			// 6. Lọc theo đánh giá sao
			if (rating.HasValue)
			{
				query = query.Where(p => p.ProductReviews.Any() &&
										 p.ProductReviews.Average(r => r.Rating) >= rating.Value);
			}

			// --- 7. LOGIC SẮP XẾP (SORT) - ĐÃ FIX LỖI DYNAMIC ---
			IOrderedQueryable<Product> sortedQuery;

			switch (sort)
			{
				case "price_asc":
					sortedQuery = query.OrderBy(p => p.SalePrice > 0 ? p.SalePrice : p.Price);
					break;
				case "price_desc":
					sortedQuery = query.OrderByDescending(p => p.SalePrice > 0 ? p.SalePrice : p.Price);
					break;
				case "rating":
					sortedQuery = query.OrderByDescending(p => p.ProductReviews.Any() ? p.ProductReviews.Average(r => r.Rating) : 0);
					break;
				case "newest":
					sortedQuery = query.OrderByDescending(p => p.Id);
					break;
				default:
					// Mặc định sắp xếp theo tổng tồn kho (logic cũ của bạn)
					sortedQuery = query.OrderByDescending(p => p.ProductSizes.Sum(ps => ps.Quantity));
					break;
			}

			// 8. Thực hiện phân trang trên Query đã sắp xếp
			var finalQuery = sortedQuery.Select(p => new {
				p.Id,
				TotalStock = p.ProductSizes.Sum(ps => ps.Quantity)
			});

			int total = await finalQuery.CountAsync();
			int totalPages = (int)Math.Ceiling((double)total / pageSize);
			page = Math.Clamp(page, 1, Math.Max(1, totalPages));

			var pagedData = await finalQuery
				.Skip((page - 1) * pageSize)
				.Take(pageSize)
				.ToListAsync();

			var pagedIds = pagedData.Select(x => x.Id).ToList();

			// 9. Lấy dữ liệu chi tiết và giữ nguyên thứ tự sắp xếp
			var products = await _context.Products
				.Include(p => p.Category)
				.Include(p => p.ProductSizes)
				.Where(p => pagedIds.Contains(p.Id))
				.ToListAsync();

			var sortedProducts = pagedIds
				.Select(id => products.First(p => p.Id == id))
				.ToList();

			foreach (var p in sortedProducts)
			{
				p.TotalQuantity = p.ProductSizes?.Sum(ps => ps.Quantity) ?? 0;
			}

			return new ProductPagedDto
			{
				Products = sortedProducts,
				TotalCount = total,
				TotalPages = totalPages,
				CurrentPage = page
			};
		}

		public async Task<List<ProductSearchResDto>> SearchQuickAsync(string? keyword, int? categoryId)
		{
			var query = _context.Products.AsQueryable();
			if (categoryId.HasValue) query = query.Where(p => p.CategoryId == categoryId);
			if (!string.IsNullOrWhiteSpace(keyword)) query = query.Where(p => p.ProductName.Contains(keyword));

			return await query.GroupBy(p => p.ProductName)
				.Select(g => new ProductSearchResDto
				{
					Id = g.First().Id,
					ProductName = g.Key,
					Price = g.First().Price,
					SalePrice = g.First().SalePrice,
					Image = g.First().Image
				}).ToListAsync();
		}

		public async Task<(Product? product, List<object> sizes)> GetProductDetailAsync(int id)
		{
			var product = await _context.Products
				.Include(p => p.Category)
				.Include(p => p.ProductSizes)
				.ThenInclude(ps => ps.Size)
				.FirstOrDefaultAsync(p => p.Id == id);

			if (product == null) return (null, new List<object>());

			product.TotalQuantity = product.ProductSizes?.Sum(s => s.Quantity) ?? 0;

			var sizes = product.ProductSizes?.Select(ps => new {
				SizeName = ps.Size.SizeName,
				Quantity = ps.Quantity,
				ProductId = ps.ProductId
			}).Cast<object>().ToList() ?? new List<object>();

			return (product, sizes);
		}
	}
}