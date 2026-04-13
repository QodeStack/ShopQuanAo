namespace ShopQuanAo.Models.DTO
{
	public class ContactsFormDto
	{
		public string FullName { get; set; }
		public string Email { get; set; }
		public string? Phone { get; set; } // Đã có Phone
		public string Message { get; set; }
	}
}