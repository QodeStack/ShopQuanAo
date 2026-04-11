using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShopQuanAo.Models
{
	[Table("ProductSize")]
	public class ProductSize
	{
		[Key]
		public int Id { get; set; }

		[Required]
		public int ProductId { get; set; }

		// Dùng virtual để hỗ trợ Lazy Loading và gán ForeignKey rõ ràng
		[ForeignKey("ProductId")]
		public virtual Product Product { get; set; }

		[Required]
		public int SizeId { get; set; }

		[ForeignKey("SizeId")]
		public virtual Size Size { get; set; }

		[Required]
		[Range(0, int.MaxValue, ErrorMessage = "Số lượng không được nhỏ hơn 0")]
		public int Quantity { get; set; }
	}
}