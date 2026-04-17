using System.ComponentModel.DataAnnotations;

namespace ShopQuanAo.Models.BEAN.Entity
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

        [Required]
        [StringLength(10)]
        public string Size { get; set; }

        public Product Product { get; set; }

        public ShoppingCart ShoppingCart { get; set; }
    }
}
