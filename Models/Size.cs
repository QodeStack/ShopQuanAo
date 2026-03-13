using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShopQuanAo.Models
{
    [Table("Size")]
    public class Size
    {
        public int Id { get; set; }

        [Required]
        [Column(TypeName = "nvarchar(10)")]
        public required string SizeName { get; set; }

        public List<ProductSize> ProductSizes { get; set; }

    }
}
