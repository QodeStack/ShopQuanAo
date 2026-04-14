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

            // 2. PHẦN QUAN TRỌNG: Lấy ID đại diện và sắp xếp theo tồn kho (SQL có thể dịch được)
            var groupedQuery = query
                .GroupBy(p => p.ProductName)
                .Select(g => new
                {
                    Id = g.Max(p => p.Id), // Lấy ID của một thằng đại diện trong nhóm
                    TotalStock = g.SelectMany(p => p.ProductSizes).Sum(ps => ps.Quantity)
                })
                .OrderByDescending(x => x.TotalStock);

            // Tính tổng số trang
            int total = await groupedQuery.CountAsync();
            int totalPages = (int)Math.Ceiling((double)total / pageSize);
            page = Math.Clamp(page, 1, Math.Max(1, totalPages));

            // Lấy danh sách ID sau khi phân trang
            var pagedData = await groupedQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var pagedIds = pagedData.Select(x => x.Id).ToList();

            // 3. Lấy đầy đủ thông tin sản phẩm từ danh sách ID đã chọn
            var products = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.ProductSizes)
                .Where(p => pagedIds.Contains(p.Id))
                .ToListAsync();

            // Sắp xếp lại danh sách products theo đúng thứ tự pagedIds (vì lệnh Where Contains làm mất thứ tự sort)
            var sortedProducts = pagedIds
                .Select(id => products.First(p => p.Id == id))
                .ToList();

            // 4. Gán giá trị vào biến ảo TotalQuantity để hiển thị ở View
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