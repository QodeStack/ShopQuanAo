using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShopQuanAo.Models
{
    [Table("Product")]
    public class Product
    {
        public int Id { get; set; }

        [StringLength(50)]
        [Column(TypeName = "nvarchar(50)")]            
        
        public required string ProductName { get; set; }

        [StringLength(50)]
        [Column(TypeName = "nvarchar(50)")]

        public required string BrandName { get; set; }

        [Required]
        public double Price { get; set; }

        [Column(TypeName ="varchar(50)")]
        public string? Image { get; set; }

        public int CategoryId { get; set; }
        public Categories Category { get; set; }
        
        public List<OrderDetail> OrderDetail { get; set; }
        public List<CartDetail> CartDetail { get; set; }
        public List<ProductSize> ProductSizes { get; set; }



    }
}
