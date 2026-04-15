using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ShopQuanAo.Models.Entity;

namespace ShopQuanAo.Data
{
	public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
	{
		public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
			: base(options)
		{
		}

		public DbSet<Product> Products { get; set; }
		public DbSet<Categories> Categories { get; set; }
		public DbSet<Order> Orders { get; set; }
		public DbSet<OrderDetail> OrderDetails { get; set; }
		public DbSet<CartDetail> CartDetails { get; set; }
		public DbSet<OrderStatus> OrderStatuses { get; set; }
		public DbSet<ShoppingCart> ShoppingCarts { get; set; }
		public DbSet<Size> Sizes { get; set; }
		public DbSet<ProductSize> ProductSizes { get; set; }
		public DbSet<Contacts> Contacts { get; set; }
		public DbSet<ProductReview> ProductReviews { get; set; } // Đã có, rất tốt!

		protected override void OnModelCreating(ModelBuilder builder)
		{
			base.OnModelCreating(builder);

			// Thêm dòng này để đảm bảo EF Core tạo bảng đúng tên bạn muốn
			builder.Entity<ProductReview>().ToTable("ProductReviews");

			// Nếu bạn có các ràng buộc khác như khóa ngoại, có thể cấu hình thêm ở đây
		}
	}
}