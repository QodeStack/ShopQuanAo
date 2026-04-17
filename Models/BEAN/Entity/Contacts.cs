using System.ComponentModel.DataAnnotations;

namespace ShopQuanAo.Models.BEAN.Entity
{
	public class Contacts
	{
		public int Id { get; set; }
		[Required(ErrorMessage = "Vui lòng nhập họ tên")]
		public string FullName { get; set; }
		[Required(ErrorMessage = "Vui lòng nhập Email")]
		[EmailAddress]
		public string Email { get; set; }
		public string? Phone { get; set; }
		[Required(ErrorMessage = "Vui lòng nhập nội dung")]
		public string Message { get; set; }
		public DateTime CreatedDate { get; set; } = DateTime.Now;
		public string? AdminReply { get; set; } // Admin trả lời tại đây
		public bool IsRead { get; set; } = false; // Đánh dấu đã xem
	}
}