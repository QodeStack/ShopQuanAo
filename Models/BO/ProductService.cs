using ShopQuanAo.Models.BEAN.DTO;
using ShopQuanAo.Models.BEAN.Entity;
using ShopQuanAo.DAO;

namespace ShopQuanAo.BO
{
    public class ProductService
    {
        private readonly ProductDAO _productDAO;

        public ProductService(ProductDAO productDAO)
        {
            _productDAO = productDAO;
        }

        // 1. Phân trang và điều phối luồng dữ liệu
        public async Task<ProductPagedDto> GetPagedProductsAsync(
    string? category,
    string? search,
    int page,
    int pageSize,
    string? price = null,
    int? rating = null,
    bool isSaleOnly = false,
    string? sort = null) // Thêm tham số sort ở đây
        {
            // Bước 1: Truyền thêm tham số 'sort' xuống DAO để xử lý sắp xếp ở mức SQL
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

        // 3. Lấy thông tin chi tiết
        public async Task<(Product? product, List<object> sizes)> GetProductDetailAsync(int id)
        {
            var product = await _productDAO.GetProductWithDetailsAsync(id);

            if (product == null) return (null, new List<object>());

            // Tính tổng số lượng
            product.TotalQuantity = product.ProductSizes?.Sum(s => s.Quantity) ?? 0;

            // Chuyển mảng Size thành anonymous object cho Frontend (chuẩn DTO)
            var sizes = product.ProductSizes?.Select(ps => new {
                SizeName = ps.Size.SizeName,
                Quantity = ps.Quantity,
                ProductId = ps.ProductId
            }).Cast<object>().ToList() ?? new List<object>();

            return (product, sizes);
        }
    }
}