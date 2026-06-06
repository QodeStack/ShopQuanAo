using ShopQuanAo.Models.BEAN.Entity;

namespace ShopQuanAo.Models.BEAN.DTO
{
    public class CustomerOrderPagedDto
    {
        public List<Order> Orders { get; set; } = new();
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public string? CurrentStatus { get; set; }
    }
    namespace ShopQuanAo.Models.BEAN.DTO
    {
        public class EditUserDto
        {
            public string Id { get; set; } = string.Empty;
            public string Role { get; set; } = "User";
        }
    }
}
