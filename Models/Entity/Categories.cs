using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShopQuanAo.Models.Entity
{
    [Table("Categories")]

    public class Categories
    {
        public int Id { get; set; }

        [StringLength(20)]
        [Column(TypeName = "nvarchar(20)")]
        public required string CategoryName { get; set; }

        public List<Product> Products { get; set; }

    }
}
