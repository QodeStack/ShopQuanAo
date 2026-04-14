using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShopQuanAo.Models.Entity
{
	[Table("Product")]
	public class Product
	{
		[Key]
		public int Id { get; set; }

		[Required]
		[StringLength(50)]
		[Column(TypeName = "nvarchar(50)")]
		public required string ProductName { get; set; }

		[Required]
		[StringLength(50)]
		[Column(TypeName = "nvarchar(50)")]
		public required string BrandName { get; set; }

		[Required]
		public double Price { get; set; }
        [Required]
        public int SalePrice { get; set; }

        [NotMapped]
		public int TotalQuantity { get; set; }

		[Column(TypeName = "varchar(500)")]
		public string? Image { get; set; }

		// Khóa ngoại Category

		public int CategoryId { get; set; }
		[ForeignKey("CategoryId")]
		public Categories Category { get; set; }

		// Navigation Properties (Nên dùng ICollection và khởi tạo sẵn)
		public virtual ICollection<OrderDetail> OrderDetail { get; set; } = new List<OrderDetail>();
		public virtual ICollection<CartDetail> CartDetail { get; set; } = new List<CartDetail>();

		// Liên kết 1 - Nhiều với bảng ProductSize (Để gom size)
		public virtual ICollection<ProductSize> ProductSizes { get; set; } = new List<ProductSize>();

        // Liên kết 1 - Nhiều với bảng ProductReview
        public virtual ICollection<ProductReview> ProductReviews { get; set; } = new List<ProductReview>();
    }
}