using ShopQuanAo.Models.Entity;

namespace ShopQuanAo.Models.DTO
{
    public class ProductPagedDto
    {
        public List<Product> Products { get; set; } = new();
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public int CurrentPage { get; set; }
    }

    public class ProductSearchResDto
    {
        public int Id { get; set; }
        public string ProductName { get; set; } = "";
        public double Price { get; set; }
        public string? Image { get; set; }
    }
}