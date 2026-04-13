namespace ShopQuanAo.Models.DTO
{

    public class AddToCartDto
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public string Size { get; set; } = "";
    }

    public class UpdateCartQtyDto
    {
        public int CartDetailId { get; set; }
        public int Quantity { get; set; }
        
    }
}