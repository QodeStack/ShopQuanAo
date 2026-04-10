using Microsoft.AspNetCore.Identity;

namespace ShopQuanAo.Models
{
	// Lớp này kế thừa IdentityUser để mở rộng thêm thuộc tính OTP
	public class ApplicationUser : IdentityUser
	{
		public string? OTPCode { get; set; }
		public DateTime? OTPExpiry { get; set; }
	}
}