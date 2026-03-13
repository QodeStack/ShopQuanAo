using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShopQuanAo.Models
{
    [Table("OrderStatus")]

    public class OrderStatus
    {
        public int Id { get; set; }
        
        [Required]        
        [StringLength(20)]
        public string StatusName { get; set; }
    }
}
