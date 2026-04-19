using System.ComponentModel.DataAnnotations;

namespace ShopQuanAo.Models.BEAN.Entity
{
    public class Voucher
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Code { get; set; } // Tên mã gõ vào (VD: FREESHIP, GIAM50K)

        public double DiscountAmount { get; set; } // Mức giảm thẳng tiền mặt (VD: 50000)

        public double MinOrderAmount { get; set; } // Đơn tối thiểu để được áp dụng (VD: 200000)

        public int Quantity { get; set; } // Số lượt dùng còn lại

        public bool IsActive { get; set; } = true; // Bật/Tắt mã
        public bool IsPublic { get; set; } = true; // Bật = Bảng popup hiển thị, Tắt = Chỉ nhập tay mới được
    }
}