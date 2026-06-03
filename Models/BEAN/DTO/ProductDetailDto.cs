using ShopQuanAo.Models.BEAN.Entity;

namespace ShopQuanAo.Models.BEAN.DTO
{
    public class ProductDetailDto
    {
        public Product Product { get; set; } = null!;
        public List<object> AvailableSizes { get; set; } = new();
        public List<ProductReview> Reviews { get; set; } = new();
        public List<Voucher> Coupons { get; set; } = new();
        public List<Product> RelatedProducts { get; set; } = new();
    }
}