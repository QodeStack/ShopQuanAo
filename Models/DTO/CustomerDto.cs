using ShopQuanAo.Models.Entity;

namespace ShopQuanAo.Models.DTO
{
    public class CustomerOrderPagedDto
    {
        public List<Order> Orders { get; set; } = new();
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public string? CurrentStatus { get; set; }
    }
}
