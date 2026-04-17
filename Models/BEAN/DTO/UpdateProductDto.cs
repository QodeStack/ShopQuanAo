namespace ShopQuanAo.Models.BEAN.DTO
{
	// Class chính để Edit sản phẩm
	public class UpdateProductDto
	{
		public int Id { get; set; }
		public string ProductName { get; set; }
		public double Price { get; set; }
		public string? BrandName { get; set; }
		public string? Image { get; set; }
		public int CategoryId { get; set; }

		// SỬA TẠI ĐÂY: Phải là SizeDtos (có chữ s) để khớp với class định nghĩa bên dưới
		public List<SizeDtos>? Sizes { get; set; }
	}

	// Class định nghĩa cấu trúc Size
	public class SizeDtos
	{
		public string SizeName { get; set; }
		public int Quantity { get; set; }
	}
}