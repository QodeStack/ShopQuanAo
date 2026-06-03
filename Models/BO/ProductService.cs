using ShopQuanAo.Models.BEAN.DTO;
using ShopQuanAo.Models.BEAN.Entity;
using ShopQuanAo.DAO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace ShopQuanAo.BO
{
    public class ProductService
    {
        private readonly ProductDAO _productDAO;

        public ProductService(ProductDAO productDAO)
        {
            _productDAO = productDAO;
        }

        #region Phân trang và Tìm kiếm
        // 1. Phân trang và điều phối luồng dữ liệu
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
            var (total, pagedIds, clampedPage) = await _productDAO.GetPagedProductIdsAsync(
                category, search, price, rating, isSaleOnly, page, pageSize, sort);

            if (!pagedIds.Any())
            {
                return new ProductPagedDto
                {
                    Products = new List<Product>(),
                    TotalCount = total,
                    TotalPages = 0,
                    CurrentPage = 1
                };
            }

            // Bước 2: Kéo dữ liệu thực tế lên từ danh sách IDs đã được sắp xếp
            var products = await _productDAO.GetProductsByIdsAsync(pagedIds);

            // Bước 3: Sắp xếp lại danh sách object 'products' theo đúng thứ tự của 'pagedIds'
            // Lưu ý quan trọng: EF Core 'Where IN' không bảo toàn thứ tự, nên bước này là bắt buộc
            var sortedProducts = pagedIds
                .Select(id => products.First(p => p.Id == id))
                .ToList();

            // Bước 4: Tính toán nghiệp vụ TotalQuantity (Tồn kho)
            foreach (var p in sortedProducts)
            {
                p.TotalQuantity = p.ProductSizes?.Sum(ps => ps.Quantity) ?? 0;
            }

            int totalPages = (int)Math.Ceiling((double)total / pageSize);

            return new ProductPagedDto
            {
                Products = sortedProducts,
                TotalCount = total,
                TotalPages = totalPages,
                CurrentPage = clampedPage
            };
        }

        // 2. Tìm kiếm nhanh
        public async Task<List<ProductSearchResDto>> SearchQuickAsync(string? keyword, int? categoryId)
        {
            return await _productDAO.SearchQuickAsync(keyword, categoryId);
        }
        #endregion

        #region Chi tiết sản phẩm và Đánh giá
        // 3. Lấy thông tin chi tiết
        public async Task<ProductDetailDto?> GetProductDetailDisplayAsync(int id)
        {
            // 1. Gọi DAO lấy sản phẩm chi tiết kèm Size
            var product = await _productDAO.GetProductWithDetailsAsync(id);
            if (product == null) return null;

            // Logic nghiệp vụ: Tính tổng tồn kho
            product.TotalQuantity = product.ProductSizes?.Sum(s => s.Quantity) ?? 0;

            // Logic nghiệp vụ: Chuẩn hóa định dạng Size cho Frontend
            var sizes = product.ProductSizes?.Select(ps => new {
                SizeName = ps.Size.SizeName,
                Quantity = ps.Quantity,
                ProductId = ps.ProductId
            }).Cast<object>().ToList() ?? new List<object>();

            // 2. Gọi DAO lấy các dữ liệu vệ tinh xung quanh
            var reviews = await _productDAO.GetReviewsByProductIdAsync(id);
            var coupons = await _productDAO.GetActivePublicVouchersAsync();

            var relatedProducts = await _productDAO.GetRelatedProductsAsync(product.CategoryId, id, 4);

            // Đóng gói vào một DTO duy nhất trả về cho Controller
            return new ProductDetailDto
            {
                Product = product,
                AvailableSizes = sizes,
                Reviews = reviews,
                Coupons = coupons,
                RelatedProducts = relatedProducts
            };
        }

        // 4. Lấy danh sách đánh giá cho NHIỀU sản phẩm (Dùng cho Index, Sale)
        public async Task<Dictionary<int, List<ProductReview>>> GetReviewsForProductsAsync(List<int> productIds)
        {
            var allReviews = new Dictionary<int, List<ProductReview>>();

            if (productIds == null || !productIds.Any())
                return allReviews;

            // Lấy tất cả review từ DAO
            var reviews = await _productDAO.GetReviewsByProductIdsAsync(productIds);

            // Gom nhóm review theo từng ProductId
            foreach (var productId in productIds)
            {
                allReviews[productId] = reviews.Where(r => r.ProductId == productId).ToList();
            }

            return allReviews;
        }

        // 5. Lấy danh sách Voucher
        public async Task<List<Voucher>> GetActiveVouchersAsync()
        {
            return await _productDAO.GetActivePublicVouchersAsync();
        }

        // 6. Hàm xử lý lưu review
        public async Task CreateProductReviewAsync(int productId, int rating, string comment, string userId, string userName)
        {
            var review = new ProductReview
            {
                ProductId = productId,
                Rating = rating,
                Comment = comment,
                UserId = userId,
                FullName = userName,
                CreatedAt = DateTime.Now
            };

            // Đẩy xuống DAO để lưu vào Database
            await _productDAO.AddReviewAsync(review);
        }
        #endregion

        #region Hỗ trợ AI & Tiện ích
        public async Task<List<Product>> GetCandidatesForAIAsync(string keyword, string color = "", double maxPrice = 0)
        {
            return await _productDAO.GetCandidatesForAIAsync(keyword, color, maxPrice);
        }

        public async Task<Dictionary<int, string>> GetAvailableCategoriesAsync()
        {
            return await _productDAO.GetAvailableCategoriesAsync();
        }

        public async Task<List<string>> GetAvailableSizesAsync()
        {
            return await _productDAO.GetAvailableSizesAsync();
        }
        #endregion
    }
}