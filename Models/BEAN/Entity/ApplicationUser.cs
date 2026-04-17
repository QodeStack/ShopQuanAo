using Microsoft.AspNetCore.Identity;

namespace ShopQuanAo.Models.BEAN.Entity
{
	// Sửa chỗ này: Kế thừa từ IdentityUser chứ không phải ApplicationUser
	public class ApplicationUser : IdentityUser
	{
		public string? OTPCode { get; set; }
		public DateTime? OTPExpiry { get; set; }

		// Bạn có thể thêm các trường khác nếu MenShop cần, ví dụ:
		// public string? FullName { get; set; }
	}
}