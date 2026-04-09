using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MimeKit;
using MailKit.Net.Smtp;
using ShopQuanAo.Data;
using ShopQuanAo.Models;

namespace ShopQuanAo.Controllers
{
	[Authorize(Roles = "Admin")]
	public class AdminController : Controller
	{
		private readonly ApplicationDbContext _context;
		private readonly UserManager<IdentityUser> _userManager;

		public AdminController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
		{
			_context = context;
			_userManager = userManager;
		}

		// ── Views ──────────────────────────────────────────────
		public IActionResult Index()
		{
			ViewBag.RecentOrders = _context.Orders
				.Include(o => o.OrderDetails)
				.OrderByDescending(o => o.CreateTime)
				.Take(10)
				.ToList();
			return View();
		}

		public IActionResult Users() => View();

		public IActionResult Products()
		{
			ViewBag.Categories = _context.Categories.ToList();
			return View();
		}

		public async Task<IActionResult> Contacts()
		{
			var contacts = await _context.Contacts
				.OrderByDescending(c => c.CreatedDate)
				.ToListAsync();
			return View(contacts);
		}

		// ── Dashboard Stats API ────────────────────────────────
		[HttpGet]
		public async Task<IActionResult> GetStats()
		{
			try
			{
				var now = DateTime.Now;
				var startOfMonth = new DateTime(now.Year, now.Month, 1);

				var revenue = await _context.OrderDetails
					.Where(od => od.Order.CreateTime >= startOfMonth && od.Order.IsPaid)
					.SumAsync(od => (double?)(od.UnitPrice * od.Quantity)) ?? 0;

				var orders = await _context.Orders
					.Where(o => o.CreateTime >= startOfMonth)
					.CountAsync();

				var users = await _userManager.Users.CountAsync();

				var products = await _context.Products
					.Select(p => p.ProductName)
					.Distinct()
					.CountAsync();

				return Json(new { revenue, orders, users, products });
			}
			catch (Exception ex)
			{
				return Json(new { error = ex.Message });
			}
		}

		// ── Contacts API ──────────────────────────────────────────

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> ReplyContact(int id, string adminReply)
		{
			try
			{
				var contact = await _context.Contacts.FindAsync(id);
				if (contact == null) return Json(new { success = false, message = "Không tìm thấy khách hàng." });

				// 1. Cập nhật Database trước
				contact.AdminReply = adminReply;
				contact.IsRead = true;
				_context.Update(contact);
				await _context.SaveChangesAsync();

				// 2. Gửi Email thực tế bằng tài khoản leythien2508@gmail.com
				try
				{
					var emailMessage = new MimeMessage();
					// Người gửi: Dùng mail thật của bạn
					emailMessage.From.Add(new MailboxAddress("MenShop Admin", "leythien2508@gmail.com"));
					// Người nhận: Lấy từ database khách hàng
					emailMessage.To.Add(new MailboxAddress(contact.FullName, contact.Email));
					emailMessage.Subject = "[MenShop] Phản hồi yêu cầu liên hệ";

					emailMessage.Body = new TextPart("plain")
					{
						Text = $"Chào {contact.FullName},\n\nAdmin MenShop xin phản hồi về nội dung của bạn như sau:\n\"{adminReply}\"\n\nCảm ơn bạn đã quan tâm đến cửa hàng chúng tôi!"
					};

					using (var client = new SmtpClient())
					{
						// Kết nối đến server Gmail
						await client.ConnectAsync("smtp.gmail.com", 587, MailKit.Security.SecureSocketOptions.StartTls);

						// Xác thực bằng mail và mật khẩu ứng dụng bạn đã tạo
						await client.AuthenticateAsync("leythien2508@gmail.com", "hszr hbjw vamm twxa");

						await client.SendAsync(emailMessage);
						await client.DisconnectAsync(true);
					}
					return Json(new { success = true, message = "Đã lưu phản hồi và gửi mail thành công!" });
				}
				catch (Exception mailEx)
				{
					// Nếu lỗi mail, vẫn báo thành công vì DB đã lưu dữ liệu phản hồi
					return Json(new { success = true, message = "Lưu thành công nhưng gửi mail thất bại: " + mailEx.Message });
				}
			}
			catch (Exception ex)
			{
				return Json(new { success = false, message = "Lỗi hệ thống: " + ex.Message });
			}
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> DeleteContact(int id)
		{
			try
			{
				var contact = await _context.Contacts.FindAsync(id);
				if (contact == null) return Json(new { success = false });

				_context.Contacts.Remove(contact);
				await _context.SaveChangesAsync();
				return Json(new { success = true });
			}
			catch (Exception ex)
			{
				return Json(new { success = false, message = ex.Message });
			}
		}

		// ── Users API ──────────────────────────────────────────
		[HttpGet]
		public async Task<IActionResult> GetUsers()
		{
			var users = await _userManager.Users.ToListAsync();
			var result = new List<object>();

			foreach (var u in users)
			{
				var roles = await _userManager.GetRolesAsync(u);
				result.Add(new
				{
					id = u.Id,
					email = u.Email,
					userName = u.UserName,
					emailConfirmed = u.EmailConfirmed,
					twoFactorEnabled = u.TwoFactorEnabled,
					roles = roles.ToList()
				});
			}
			return Json(result);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> CreateUser([FromBody] CreateUserDto dto)
		{
			if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
				return Json(new { success = false, message = "Email và mật khẩu không được để trống." });

			var user = new IdentityUser { UserName = dto.Email, Email = dto.Email, EmailConfirmed = true };
			var result = await _userManager.CreateAsync(user, dto.Password);

			if (!result.Succeeded)
				return Json(new { success = false, message = string.Join(", ", result.Errors.Select(e => e.Description)) });

			if (!string.IsNullOrEmpty(dto.Role))
				await _userManager.AddToRoleAsync(user, dto.Role);

			return Json(new { success = true });
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> EditUser([FromBody] EditUserDto dto)
		{
			var user = await _userManager.FindByIdAsync(dto.Id);
			if (user == null) return Json(new { success = false, message = "Không tìm thấy người dùng." });

			var currentRoles = await _userManager.GetRolesAsync(user);
			await _userManager.RemoveFromRolesAsync(user, currentRoles);
			if (!string.IsNullOrEmpty(dto.Role))
				await _userManager.AddToRoleAsync(user, dto.Role);

			return Json(new { success = true });
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> DeleteUser([FromBody] DeleteDto dto)
		{
			var currentUser = await _userManager.GetUserAsync(User);
			if (currentUser?.Id == dto.Id)
				return Json(new { success = false, message = "Không thể xóa tài khoản đang đăng nhập." });

			var user = await _userManager.FindByIdAsync(dto.Id);
			if (user == null) return Json(new { success = false, message = "Không tìm thấy người dùng." });

			var result = await _userManager.DeleteAsync(user);
			return Json(result.Succeeded ? new { success = true } : new { success = false, message = "Lỗi khi xóa." });
		}

		// ── Products API ──────────────────────────────────────────
		[HttpGet]
		public async Task<IActionResult> GetProducts()
		{
			try
			{
				var products = await _context.Products
					.Include(p => p.Category)
					.Include(p => p.ProductSizes).ThenInclude(ps => ps.Size)
					.Select(p => new {
						id = p.Id,
						productName = p.ProductName,
						brandName = p.BrandName,
						price = p.Price,
						image = p.Image,
						categoryId = p.CategoryId,
						categoryName = p.Category.CategoryName,
						productSizes = p.ProductSizes
							.OrderBy(ps => ps.Size.SizeName)
							.Select(ps => new {
								id = ps.Id,
								sizeName = ps.Size.SizeName,
								quantity = ps.Quantity
							}).ToList()
					})
					.ToListAsync();

				return Json(products);
			}
			catch (Exception ex)
			{
				return Json(new { error = "Lỗi: " + ex.Message });
			}
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> CreateProduct([FromBody] CreateProductWithSizesDto dto)
		{
			try
			{
				if (string.IsNullOrWhiteSpace(dto.ProductName))
					return Json(new { success = false, message = "Tên sản phẩm không trống." });

				var isExist = await _context.Products.AnyAsync(p => p.ProductName.ToLower() == dto.ProductName.ToLower());
				if (isExist)
					return Json(new { success = false, message = "Sản phẩm này đã tồn tại mẫu." });

				var product = new Product
				{
					ProductName = dto.ProductName,
					BrandName = dto.BrandName ?? "MenShop",
					Price = dto.Price,
					Image = dto.Image,
					CategoryId = dto.CategoryId
				};

				_context.Products.Add(product);
				await _context.SaveChangesAsync();

				foreach (var sizeItem in dto.Sizes.Where(s => s.Quantity >= 0))
				{
					var size = await _context.Sizes.FirstOrDefaultAsync(s => s.SizeName == sizeItem.SizeName);
					if (size == null)
					{
						size = new Size { SizeName = sizeItem.SizeName.ToUpper() };
						_context.Sizes.Add(size);
						await _context.SaveChangesAsync();
					}

					_context.ProductSizes.Add(new ProductSize
					{
						ProductId = product.Id,
						SizeId = size.Id,
						Quantity = sizeItem.Quantity
					});
				}
				await _context.SaveChangesAsync();

				return Json(new { success = true, message = "Thêm sản phẩm thành công!" });
			}
			catch (Exception ex)
			{
				return Json(new { success = false, message = "Lỗi: " + ex.Message });
			}
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> EditProduct([FromBody] ProductDto dto)
		{
			try
			{
				var product = await _context.Products.FindAsync(dto.Id);
				if (product == null) return Json(new { success = false, message = "Không tìm thấy." });

				product.ProductName = dto.ProductName;
				product.BrandName = dto.BrandName ?? product.BrandName;
				product.Price = dto.Price;
				product.Image = dto.Image;
				product.CategoryId = dto.CategoryId;

				await _context.SaveChangesAsync();
				return Json(new { success = true, message = "Cập nhật thành công!" });
			}
			catch (Exception ex)
			{
				return Json(new { success = false, message = ex.Message });
			}
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> DeleteProduct([FromBody] DeleteIntDto dto)
		{
			try
			{
				var product = await _context.Products.FindAsync(dto.Id);
				if (product == null) return Json(new { success = false, message = "Không tìm thấy." });

				_context.Products.Remove(product);
				await _context.SaveChangesAsync();
				return Json(new { success = true, message = "Xóa thành công!" });
			}
			catch (Exception ex)
			{
				return Json(new { success = false, message = ex.Message });
			}
		}

		// ── Product Sizes API ──────────────────────────────────────
		[HttpGet]
		public async Task<IActionResult> GetProductSizes(int productId)
		{
			var productSizes = await _context.ProductSizes
				.Where(ps => ps.ProductId == productId)
				.Include(ps => ps.Size)
				.Select(ps => new {
					id = ps.Id,
					sizeName = ps.Size.SizeName,
					quantity = ps.Quantity
				}).ToListAsync();
			return Json(productSizes);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> UpdateProductSize([FromBody] UpdateProductSizeDto dto)
		{
			var productSize = await _context.ProductSizes.FindAsync(dto.ProductSizeId);
			if (productSize == null) return Json(new { success = false, message = "Không tìm thấy." });

			productSize.Quantity = dto.Quantity;
			await _context.SaveChangesAsync();
			return Json(new { success = true });
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> DeleteProductSize([FromBody] DeleteProductSizeDto dto)
		{
			var productSize = await _context.ProductSizes.FindAsync(dto.ProductSizeId);
			if (productSize == null) return Json(new { success = false, message = "Không tìm thấy." });

			_context.ProductSizes.Remove(productSize);
			await _context.SaveChangesAsync();
			return Json(new { success = true });
		}
	}

	// ── DTOs (Data Transfer Objects) ──
	public class CreateUserDto { public string Email { get; set; } public string Password { get; set; } public string Role { get; set; } }
	public class EditUserDto { public string Id { get; set; } public string Role { get; set; } }
	public class DeleteDto { public string Id { get; set; } }
	public class DeleteIntDto { public int Id { get; set; } }
	public class ProductDto { public int Id { get; set; } public string ProductName { get; set; } public string BrandName { get; set; } public double Price { get; set; } public string Image { get; set; } public int CategoryId { get; set; } }
	public class CreateProductWithSizesDto { public string ProductName { get; set; } public string BrandName { get; set; } public double Price { get; set; } public string Image { get; set; } public int CategoryId { get; set; } public List<SizeDto> Sizes { get; set; } }
	public class SizeDto { public string SizeName { get; set; } public int Quantity { get; set; } }
	public class UpdateProductSizeDto { public int ProductSizeId { get; set; } public int Quantity { get; set; } }
	public class DeleteProductSizeDto { public int ProductSizeId { get; set; } }
}