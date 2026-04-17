namespace ShopQuanAo.Models.BEAN.DTO
{
    public class PlaceOrderDto
    {
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
        public string MobileNumber { get; set; } = "";
        public string Address { get; set; } = "";
        public string? Note { get; set; }
        public string PaymentMethod { get; set; } = "COD";
        public List<int> SelectedIds { get; set; } = new List<int>();
    }
}