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

        public async Task<ProductPagedDto> GetPagedProductsAsync(string? category, string? search, int page, int pageSize, string? price = null, int? rating = null)
        {
            var query = _context.Products.Include(p => p.Category).AsQueryable();

            // Lọc theo danh mục (ID số hoặc tên)
            if (!string.IsNullOrWhiteSpace(category))
            {
                if (int.TryParse(category, out int id))
                    query = query.Where(p => p.CategoryId == id);
                else
                    query = query.Where(p => p.Category.CategoryName.Contains(category));
            }

            // Lọc theo từ khóa tìm kiếm
            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(p => p.ProductName.Contains(search) || p.BrandName.Contains(search));

            // Lọc theo giá
            query = price switch
            {
                "under500" => query.Where(p => p.Price < 500000),
                "500to1000" => query.Where(p => p.Price >= 500000 && p.Price <= 1000000),
                "1000to2000" => query.Where(p => p.Price > 1000000 && p.Price <= 2000000),
                "over2000" => query.Where(p => p.Price > 2000000),
                _ => query
            };
            if (rating.HasValue)
            {
                // Lọc những sản phẩm có ít nhất 1 đánh giá VÀ điểm trung bình >= mức chọn
                query = query.Where(p => p.ProductReviews.Any() &&
                                         p.ProductReviews.Average(r => r.Rating) >= rating.Value);
            }

            var groupedQuery = query.GroupBy(p => p.ProductName).Select(g => g.First());

            int total = await groupedQuery.CountAsync();
            int totalPages = (int)Math.Ceiling((double)total / pageSize);
            page = Math.Clamp(page, 1, Math.Max(1, totalPages));

            var products = await groupedQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new ProductPagedDto
            {
                Products = products,
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
                    Image = g.First().Image
                }).ToListAsync();
        }

        public async Task<(Product? product, List<object> sizes)> GetProductDetailAsync(int id)
        {
            var product = await _context.Products.Include(p => p.Category).FirstOrDefaultAsync(p => p.Id == id);
            if (product == null) return (null, new List<object>());

            var sizes = await _context.Products
                .Where(p => p.ProductName == product.ProductName)
                .Include(p => p.ProductSizes).ThenInclude(ps => ps.Size)
                .SelectMany(p => p.ProductSizes)
                .Select(ps => new {
                    SizeName = ps.Size.SizeName,
                    Quantity = ps.Quantity,
                    ProductId = ps.ProductId
                }).Cast<object>().ToListAsync();

            return (product, sizes);
        }

    }
}