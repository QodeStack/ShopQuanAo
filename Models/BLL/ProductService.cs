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

        // 1. Thêm tham số isSaleOnly để dùng chung cho cả trang Sản phẩm và trang Sale
        public async Task<ProductPagedDto> GetPagedProductsAsync(string? category, string? search, int page, int pageSize, string? price = null, int? rating = null, bool isSaleOnly = false)
        {
            var query = _context.Products.Include(p => p.Category).AsQueryable();

            // LOGIC SALE: Nếu là trang Sale, chỉ lấy những thằng có SalePrice > 0
            if (isSaleOnly)
            {
                query = query.Where(p => p.SalePrice > 0);
            }

            // Lọc theo danh mục
            if (!string.IsNullOrWhiteSpace(category))
            {
                if (int.TryParse(category, out int id))
                    query = query.Where(p => p.CategoryId == id);
                else
                    query = query.Where(p => p.Category.CategoryName.Contains(category));
            }

            // Lọc theo từ khóa
            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(p => p.ProductName.Contains(search) || p.BrandName.Contains(search));

            // Lọc theo giá (Lưu ý: Nếu đang sale, có thể mày muốn lọc theo SalePrice thay vì Price gốc)
            query = price switch
            {
                "under500" => query.Where(p => (isSaleOnly ? p.SalePrice : p.Price) < 500000),
                "500to1000" => query.Where(p => (isSaleOnly ? p.SalePrice : p.Price) >= 500000 && (isSaleOnly ? p.SalePrice : p.Price) <= 1000000),
                "1000to2000" => query.Where(p => (isSaleOnly ? p.SalePrice : p.Price) > 1000000 && (isSaleOnly ? p.SalePrice : p.Price) <= 2000000),
                "over2000" => query.Where(p => (isSaleOnly ? p.SalePrice : p.Price) > 2000000),
                _ => query
            };

            if (rating.HasValue)
            {
                query = query.Where(p => p.ProductReviews.Any() &&
                                         p.ProductReviews.Average(r => r.Rating) >= rating.Value);
            }

            // Grouping logic giữ nguyên của mày
            var groupedQuery = query
                .GroupBy(p => p.ProductName)
                .Select(g => new
                {
                    Id = g.Max(p => p.Id),
                    TotalStock = g.SelectMany(p => p.ProductSizes).Sum(ps => ps.Quantity)
                })
                .OrderByDescending(x => x.TotalStock);

            int total = await groupedQuery.CountAsync();
            int totalPages = (int)Math.Ceiling((double)total / pageSize);
            page = Math.Clamp(page, 1, Math.Max(1, totalPages));

            var pagedData = await groupedQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var pagedIds = pagedData.Select(x => x.Id).ToList();

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

        // 2. Cập nhật Search nhanh để trả về cả giá Sale
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
                    SalePrice = g.First().SalePrice, // Mapping thêm cột này vào DTO
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