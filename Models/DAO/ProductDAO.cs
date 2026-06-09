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

        #region Xử lý danh sách và Phân trang sản phẩm
        // Lấy tổng số lượng và danh sách ID sản phẩm đã được phân trang
        public async Task<(int Total, List<int> PagedIds, int ClampedPage)> GetPagedProductIdsAsync(string? category, string? search, string? price, int? rating, bool isSaleOnly, int page, int pageSize, string? sort)
        {
            var query = _context.Products.Include(p => p.Category).AsQueryable();

            if (!string.IsNullOrWhiteSpace(category))
            {
                if (int.TryParse(category, out int catId))
                    query = query.Where(p => p.CategoryId == catId);
                else
                    query = query.Where(p => p.Category.CategoryName.Contains(category));
            }
            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(p => p.ProductName.Contains(search) || p.BrandName.Contains(search));

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
        // Giá hiện tại: dùng SalePrice nếu có, không thì dùng Price
        CurrentPrice = g.Max(p => p.SalePrice > 0 && p.SalePrice < p.Price ? p.SalePrice : p.Price),
        AvgRating = g.SelectMany(p => p.ProductReviews).Any() ? g.SelectMany(p => p.ProductReviews).Average(r => r.Rating) : 0,
        CreatedAt = g.Max(p => p.Id)
    });

            // 6. Thực hiện Sort
            groupedQuery = sort switch
            {
                "price_asc" => groupedQuery.OrderBy(x => x.CurrentPrice),
                "price_desc" => groupedQuery.OrderByDescending(x => x.CurrentPrice),
                "rating" => groupedQuery.OrderByDescending(x => x.AvgRating),
                "newest" => groupedQuery.OrderByDescending(x => x.CreatedAt),
                _ => groupedQuery.OrderByDescending(x => x.TotalStock)
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

        // Tìm kiếm nhanh trả thẳng về DTO
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
        #endregion

        #region Chi tiết sản phẩm và Thành phần liên quan
        public async Task<Product?> GetProductWithDetailsAsync(int id)
        {
            return await _context.Products
                .Include(p => p.Category)
                .Include(p => p.ProductSizes)
                .ThenInclude(ps => ps.Size)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        // 1. Lấy danh sách đánh giá của 1 sản phẩm (Dùng cho trang Chi tiết)
        public async Task<List<ProductReview>> GetReviewsByProductIdAsync(int productId)
        {
            return await _context.ProductReviews
                .Where(r => r.ProductId == productId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        // 2. Lấy danh sách đánh giá của NHIỀU sản phẩm (Dùng cho trang Index, Sale)
        public async Task<List<ProductReview>> GetReviewsByProductIdsAsync(List<int> productIds)
        {
            if (productIds == null || !productIds.Any())
                return new List<ProductReview>();

            return await _context.ProductReviews
                .Where(r => productIds.Contains(r.ProductId))
                .ToListAsync();
        }

        // 3. Lấy danh sách Voucher công khai, còn hạn
        public async Task<List<Voucher>> GetActivePublicVouchersAsync()
        {
            return await _context.Vouchers
                .Where(v => v.IsActive == true && v.Quantity > 0 && v.IsPublic == true)
                .OrderBy(v => v.MinOrderAmount)
                .ToListAsync();
        }

        // 4. Lấy sản phẩm liên quan cùng danh mục (loại trừ sản phẩm đang xem)
        public async Task<List<Product>> GetRelatedProductsAsync(int categoryId, int excludeProductId, int take = 4)
        {
            return await _context.Products
                .Where(p => p.CategoryId == categoryId && p.Id != excludeProductId)
                .OrderByDescending(p => p.Id)
                .Take(take)
                .ToListAsync();
        }

        // 5. Lưu đánh giá mới vào DB
        public async Task AddReviewAsync(ProductReview review)
        {
            _context.ProductReviews.Add(review);
            await _context.SaveChangesAsync();
        }
        #endregion

        #region Hỗ trợ AI & Tiện ích khác
        public async Task<List<Product>> GetCandidatesForAIAsync(string keyword, string color = "", double maxPrice = 0)
        {
            if (string.IsNullOrWhiteSpace(keyword)) return new List<Product>();
            string searchKey = keyword.ToLower();

            var query = _context.Products.AsQueryable();

            query = query.Where(p => p.ProductName.ToLower().Contains(searchKey)
                                  || (p.Description != null && p.Description.ToLower().Contains(searchKey)));

            if (!string.IsNullOrEmpty(color))
            {
                string colorKey = color.ToLower();
                query = query.Where(p => p.ProductName.ToLower().Contains(colorKey)
                                      || (p.Description != null && p.Description.ToLower().Contains(colorKey)));
            }

            if (maxPrice > 0)
            {
                query = query.Where(p => (p.SalePrice > 0 ? p.SalePrice : p.Price) <= maxPrice);
            }

            return await query.Take(15).ToListAsync();
        }

        public async Task<Dictionary<int, string>> GetAvailableCategoriesAsync()
        {
            return await _context.Categories
                .ToDictionaryAsync(c => c.Id, c => c.CategoryName);
        }

        public async Task<List<string>> GetAvailableSizesAsync()
        {
            return await _context.ProductSizes
                .Select(ps => ps.Size.SizeName)
                .Distinct()
                .ToListAsync();
        }
        #endregion
    }
}