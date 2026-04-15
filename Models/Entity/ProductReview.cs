using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShopQuanAo.Models.Entity
{
	[Table("ProductReviews")]
	public class ProductReview
	{
		[Key]
		public int Id { get; set; }

		[Required]
		public int ProductId { get; set; }

		[ForeignKey("ProductId")]
		public virtual Product? Product { get; set; }

		// Đổi thành Nullable (?) để không bị lỗi Validate khi gửi Form
		public string? UserId { get; set; }

		[StringLength(100)]
		public string? FullName { get; set; }

		[Range(1, 5, ErrorMessage = "Vui lòng chọn số sao từ 1 đến 5")]
		public int Rating { get; set; }

		[Required(ErrorMessage = "Vui lòng nhập nội dung đánh giá")]
		public string Comment { get; set; }

		public DateTime CreatedAt { get; set; } = DateTime.Now;
	}
}