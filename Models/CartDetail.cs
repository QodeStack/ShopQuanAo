using System.ComponentModel.DataAnnotations;

namespace ShopQuanAo.Models
{
    public class CartDetail
    {
        public int Id { get; set; }

        [Required]
        public int ShoppingCartId { get; set; }

        [Required]
        public int ProductId { get; set; }

        [Required]
        public int Quantity { get; set; }

        [Required]
        public double UnitPrice { get; set; }
        
        public Product Product { get; set; }

        public ShoppingCart ShoppingCart { get; set; }
    }
}
