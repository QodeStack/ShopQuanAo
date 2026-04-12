using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ShopQuanAo.Models.Entity;

namespace ShopQuanAo.Data
{
	// Sửa IdentityDbContext thành IdentityDbContext<ApplicationUser> để hỗ trợ thêm cột OTP
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

		protected override void OnModelCreating(ModelBuilder builder)
		{
			base.OnModelCreating(builder);
			// Bạn có thể thêm các cấu hình Fluent API ở đây nếu cần
		}
	}
}