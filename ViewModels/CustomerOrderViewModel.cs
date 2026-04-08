using ShopQuanAo.Models;

namespace ShopQuanAo.ViewModels
{
    public class CustomerOrderViewModel
    {
        public List<Order> Orders { get; set; } = new();
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public string CurrentStatus { get; set; } = "";
    }
}
