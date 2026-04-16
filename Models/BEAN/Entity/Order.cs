using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShopQuanAo.Models.BEAN.Entity
{
    [Table("Order")]

    public class Order
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public DateTime CreateTime { get; set; } = DateTime.Now;
        
        [Required]
        public bool IsDeleted { get; set; }

        [Required]
        [StringLength(50)]
        public string? Name { get; set; }

        [Required]
        [StringLength(50)]
        [EmailAddress]
        public string? Email { get; set; }

        [Required]
        [StringLength(20)]
        public string? MobileNumber { get; set; }

        [Required]
        [StringLength(200)]
        public string? Address { get; set; }

        [Required]
        public string PaymentMethod { get; set; } = "";

        public bool IsPaid { get; set; }

        public int OrderStatusId { get; set; }
        public OrderStatus OrderStatus { get; set; }

        public List<OrderDetail> OrderDetails { get; set; }




    }
}
 