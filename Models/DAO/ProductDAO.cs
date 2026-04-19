using Microsoft.EntityFrameworkCore;
using ShopQuanAo.Data;
using ShopQuanAo.Models.BEAN.DTO;
using ShopQuanAo.Models.BEAN.Entity;

namespace ShopQuanAo.DAO
{
    public class ProductDAO
    {
        private readonly ApplicationDbContext _context;

        public ProductDAO(ApplicationDbContext context)
        {
            _context = context;
        }

        // Lấy tổng số lượng và danh sách ID sản phẩm đã được phân trang
        public async Task<(int Total, List<int> PagedIds, int ClampedPage)> GetPagedProductIdsAsync(
    string? category, string? search, string? price, int? rating, bool isSaleOnly,
    int page, int pageSize, string? sort)
        {
            var query = _context.Products.Include(p => p.Category).AsQueryable();

            // 1. Lọc theo Category
            if (!string.IsNullOrWhiteSpace(category))
            {
                if (int.TryParse(category, out int catId))
                    query = query.Where(p => p.CategoryId == catId);
                else
                    query = query.Where(p => p.Category.CategoryName.Contains(category));
            }

            // 2. Lọc theo Search keyword
            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(p => p.ProductName.Contains(search) || p.BrandName.Contains(search));

            // 3. Lọc theo Price
            query = price switch
            {
                "under500" => query.Where(p => (isSaleOnly ? p.SalePrice : p.Price) < 500000),
                "500to1000" => query.Where(p => (isSaleOnly ? p.SalePrice : p.Price) >= 500000 && (isSaleOnly ? p.SalePrice : p.Price) <= 1000000),
                "1000to2000" => query.Where(p => (isSaleOnly ? p.SalePrice : p.Price) > 1000000 && (isSaleOnly ? p.SalePrice : p.Price) <= 2000000),
                "over2000" => query.Where(p => (isSaleOnly ? p.SalePrice : p.Price) > 2000000),
                _ => query
            };

            // 4. Lọc theo Sale (Nếu trang Sale gọi)
            if (isSaleOnly) query = query.Where(p => p.SalePrice > 0 && p.Price > p.SalePrice);

            // 5. GroupBy để lấy thông tin tổng hợp cho Sắp xếp
            var groupedQuery = query
                .GroupBy(p => p.ProductName)
                .Select(g => new
                {
                    Id = g.Max(p => p.Id),
                    TotalStock = g.SelectMany(p => p.ProductSizes).Sum(ps => ps.Quantity),
                    MaxPrice = g.Max(p => p.Price),
                    AvgRating = g.SelectMany(p => p.ProductReviews).Any() ? g.SelectMany(p => p.ProductReviews).Average(r => r.Rating) : 0,
                    CreatedAt = g.Max(p => p.Id)
                });

            // 6. Thực hiện Sort (Quan trọng để chạy được trên giao diện)
            groupedQuery = sort switch
            {
                "price_asc" => groupedQuery.OrderBy(x => x.MaxPrice),
                "price_desc" => groupedQuery.OrderByDescending(x => x.MaxPrice),
                "rating" => groupedQuery.OrderByDescending(x => x.AvgRating),
                "newest" => groupedQuery.OrderByDescending(x => x.CreatedAt),
                _ => groupedQuery.OrderByDescending(x => x.TotalStock) // Mặc định xếp theo tồn kho/phổ biến
            };

            int total = await groupedQuery.CountAsync();
            int totalPages = (int)Math.Ceiling((double)total / pageSize);
            page = Math.Clamp(page, 1, Math.Max(1, totalPages));

            var pagedIds = await groupedQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => x.Id)
                .ToListAsync();

            return (total, pagedIds, page);
        }

        // Truy xuất chi tiết sản phẩm dựa trên danh sách ID đã chốt
        public async Task<List<Product>> GetProductsByIdsAsync(List<int> ids)
        {
            return await _context.Products
                .Include(p => p.Category)
                .Include(p => p.ProductSizes)
                .Where(p => ids.Contains(p.Id))
                .ToListAsync();
        }

        // Tìm kiếm nhanh trả thẳng về DTO để tối ưu GroupBy trong EF Core
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

        public async Task<Product?> GetProductWithDetailsAsync(int id)
        {
            return await _context.Products
                .Include(p => p.Category)
                .Include(p => p.ProductSizes)
                .ThenInclude(ps => ps.Size)
                .FirstOrDefaultAsync(p => p.Id == id);
        }
    }
}