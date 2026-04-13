using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MailKit.Net.Smtp;
using MimeKit;
using ShopQuanAo.Data;
using ShopQuanAo.Models.Entity;
using ShopQuanAo.Models.DTO;

namespace ShopQuanAo.Services
{
	public class AdminService
	{
		private readonly ApplicationDbContext _context;
		private readonly UserManager<ApplicationUser> _userManager;

		public AdminService(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
		{
			_context = context;
			_userManager = userManager;
		}

		#region Helper Methods (Email & OTP)
		public string GenerateOTP() => new Random().Next(100000, 999999).ToString();

		public async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
		{
			var message = new MimeMessage();
			message.From.Add(new MailboxAddress("MenShop System", "leythien2508@gmail.com"));
			message.To.Add(new MailboxAddress("", toEmail));
			message.Subject = subject;

			var bodyBuilder = new BodyBuilder { HtmlBody = htmlBody };
			message.Body = bodyBuilder.ToMessageBody();

			using var client = new SmtpClient();
			await client.ConnectAsync("smtp.gmail.com", 587, MailKit.Security.SecureSocketOptions.StartTls);
			await client.AuthenticateAsync("leythien2508@gmail.com", "hszr hbjw vamm twxa");
			await client.SendAsync(message);
			await client.DisconnectAsync(true);
		}
		#endregion

		#region Dashboard & Stats
		public async Task<object> GetStatsAsync()
		{
			var now = DateTime.Now;
			var startOfMonth = new DateTime(now.Year, now.Month, 1);
			var revenue = await _context.OrderDetails
				.Where(od => od.Order.CreateTime >= startOfMonth && od.Order.IsPaid)
				.SumAsync(od => (double?)(od.UnitPrice * od.Quantity)) ?? 0;

			return new
			{
				revenue,
				orders = await _context.Orders.CountAsync(o => o.CreateTime >= startOfMonth),
				users = await _userManager.Users.CountAsync(),
				products = await _context.Products.Select(p => p.ProductName).Distinct().CountAsync()
			};
		}
		#endregion

		#region User Management
		public async Task<List<object>> GetUsersAsync()
		{
			var users = await _userManager.Users.ToListAsync();
			var result = new List<object>();
			foreach (var u in users)
			{
				var roles = await _userManager.GetRolesAsync(u);
				result.Add(new { id = u.Id, email = u.Email, userName = u.UserName, emailConfirmed = u.EmailConfirmed, twoFactorEnabled = u.TwoFactorEnabled, roles });
			}
			return result;
		}

		public async Task<(bool Success, string Message)> CreateUserAsync(CreateUserDto dto)
		{
			var user = new ApplicationUser
			{
				UserName = dto.Email,
				Email = dto.Email,
				EmailConfirmed = false,
				OTPCode = GenerateOTP(),
				OTPExpiry = DateTime.Now.AddMinutes(5)
			};
			var result = await _userManager.CreateAsync(user, dto.Password);
			if (!result.Succeeded) return (false, string.Join(", ", result.Errors.Select(e => e.Description)));

			await SendEmailAsync(user.Email, "Mã xác thực MenShop", $"Mã OTP của bạn là: <b>{user.OTPCode}</b>");
			if (!string.IsNullOrEmpty(dto.Role)) await _userManager.AddToRoleAsync(user, dto.Role);
			return (true, "Đã gửi mã OTP vào email!");
		}

		public async Task<bool> DeleteUserAsync(string id, string currentUserId)
		{
			if (id == currentUserId) return false;
			var user = await _userManager.FindByIdAsync(id);
			if (user == null) return false;

			var carts = _context.ShoppingCarts.Where(c => c.UserId == id);
			_context.ShoppingCarts.RemoveRange(carts);
			await _userManager.DeleteAsync(user);
			return true;
		}
		#endregion

		#region Product & Size Management
		public async Task<object> GetAllProductsAsync()
		{
			return await _context.Products
				.Include(p => p.Category)
				.Include(p => p.ProductSizes)
				.ThenInclude(ps => ps.Size)
				.Select(p => new {
					id = p.Id,
					productName = p.ProductName,
					brandName = p.BrandName,
					price = p.Price,
					image = p.Image,
					categoryId = p.CategoryId,
					categoryName = p.Category != null ? p.Category.CategoryName : "N/A",
					productSizes = p.ProductSizes.Select(ps => new { id = ps.Id, sizeName = ps.Size.SizeName, quantity = ps.Quantity }).ToList()
				}).ToListAsync();
		}

		public async Task<(bool Success, string Message)> CreateProductAsync(CreateProductWithSizesDto dto)
		{
			if (await _context.Products.AnyAsync(p => p.ProductName.ToLower() == dto.ProductName.ToLower()))
				return (false, "Sản phẩm đã tồn tại.");

			var product = new Product { ProductName = dto.ProductName, BrandName = dto.BrandName ?? "MenShop", Price = dto.Price, Image = dto.Image, CategoryId = dto.CategoryId };
			_context.Products.Add(product);
			await _context.SaveChangesAsync();

			if (dto.Sizes != null)
			{
				foreach (var s in dto.Sizes)
				{
					var size = await _context.Sizes.FirstOrDefaultAsync(x => x.SizeName == s.SizeName.ToUpper()) ?? new Size { SizeName = s.SizeName.ToUpper() };
					if (size.Id == 0) _context.Sizes.Add(size);
					await _context.SaveChangesAsync();
					_context.ProductSizes.Add(new ProductSize { ProductId = product.Id, SizeId = size.Id, Quantity = s.Quantity });
				}
				await _context.SaveChangesAsync();
			}
			return (true, "Thành công");
		}

		public async Task<(bool Success, string Message)> UpdateProductAsync(UpdateProductDto dto)
		{
			var product = await _context.Products.Include(p => p.ProductSizes).FirstOrDefaultAsync(p => p.Id == dto.Id);
			if (product == null) return (false, "Không tìm thấy sản phẩm.");

			try
			{
				product.ProductName = dto.ProductName;
				product.Price = dto.Price;
				product.BrandName = dto.BrandName ?? "MenShop";
				product.CategoryId = dto.CategoryId;
				if (!string.IsNullOrEmpty(dto.Image)) product.Image = dto.Image;

				_context.ProductSizes.RemoveRange(product.ProductSizes);
				await _context.SaveChangesAsync();

				if (dto.Sizes != null)
				{
					foreach (var s in dto.Sizes)
					{
						var size = await _context.Sizes.FirstOrDefaultAsync(x => x.SizeName == s.SizeName.ToUpper())
								   ?? new Size { SizeName = s.SizeName.ToUpper() };
						if (size.Id == 0) _context.Sizes.Add(size);
						await _context.SaveChangesAsync();

						_context.ProductSizes.Add(new ProductSize { ProductId = product.Id, SizeId = size.Id, Quantity = s.Quantity });
					}
				}
				await _context.SaveChangesAsync();
				return (true, "Thành công");
			}
			catch (Exception ex)
			{
				return (false, "Lỗi SQL: " + (ex.InnerException?.Message ?? ex.Message));
			}
		}

		public async Task<bool> DeleteProductAsync(int id)
		{
			var product = await _context.Products.FindAsync(id);
			if (product == null) return false;

			try
			{
				var orderDetails = _context.OrderDetails.Where(od => od.ProductId == id);
				_context.OrderDetails.RemoveRange(orderDetails);

				var productSizes = _context.ProductSizes.Where(ps => ps.ProductId == id);
				_context.ProductSizes.RemoveRange(productSizes);

				var cartDetails = _context.CartDetails.Where(cd => cd.ProductId == id);
				_context.CartDetails.RemoveRange(cartDetails);

				_context.Products.Remove(product);

				await _context.SaveChangesAsync();
				return true;
			}
			catch (Exception)
			{
				return false;
			}
		}
		#endregion

		#region Order & Contact Management
		public async Task<List<object>> GetOrdersAsync()
		{
			return await _context.Orders.Include(o => o.OrderStatus).Include(o => o.OrderDetails).ThenInclude(od => od.Product)
				.Where(o => !o.IsDeleted).OrderByDescending(o => o.CreateTime)
				.Select(o => new {
					id = o.Id,
					name = o.Name,
					email = o.Email,
					paymentMethod = o.PaymentMethod,
					isPaid = o.IsPaid,
					createTime = o.CreateTime,
					statusName = o.OrderStatus.StatusName,
					orderDetails = o.OrderDetails.Select(d => new { d.Id, d.Quantity, d.UnitPrice, productName = d.Product.ProductName }).ToList()
				}).Cast<object>().ToListAsync();
		}

		public async Task<(bool Success, string NewStatus)> UpdateOrderStatusAsync(UpdateOrderStatusDto dto)
		{
			var order = await _context.Orders.Include(o => o.OrderStatus).FirstOrDefaultAsync(o => o.Id == dto.OrderId);
			if (order == null) return (false, "");

			string? nextStatusName = dto.Action switch
			{
				"confirm" when order.OrderStatus.StatusName == "Chờ xác nhận" => "Đang xử lý",
				"ship" when order.OrderStatus.StatusName == "Đang xử lý" => "Đang giao hàng",
				"cancel" when order.OrderStatus.StatusName == "Chờ xác nhận" => "Đã hủy",
				_ => null
			};

			if (nextStatusName == null) return (false, "");
			var status = await _context.OrderStatuses.FirstAsync(s => s.StatusName == nextStatusName);
			order.OrderStatusId = status.Id;
			await _context.SaveChangesAsync();
			return (true, nextStatusName);
		}

		public async Task<bool> DeleteContactAsync(int id)
		{
			try
			{
				var contact = await _context.Contacts.FindAsync(id);
				if (contact == null) return false;

				_context.Contacts.Remove(contact);
				await _context.SaveChangesAsync();
				return true;
			}
			catch
			{
				return false;
			}
		}

		public async Task SendReplyContactEmailAsync(string toEmail, string fullName, string replyMessage)
		{
			string finalMessage = string.IsNullOrWhiteSpace(replyMessage) ? "Cảm ơn bạn đã liên hệ với chúng tôi." : replyMessage;
			string subject = "Phản hồi từ Ban quản trị MenShop";
			string body = $@"
                <div style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
                    <h2 style='color: #78ae2d;'>Chào {fullName},</h2>
                    <p>Chúng tôi đã nhận được yêu cầu của bạn. Admin MenShop xin phản hồi nội dung như sau:</p>
                    <div style='background: #f9f9f9; padding: 20px; border-left: 5px solid #78ae2d; margin: 20px 0; font-style: italic;'>
                        ""{finalMessage}""
                    </div>
                    <p>Trân trọng,<br/><b>Đội ngũ MenShop</b></p>
                </div>";
			await SendEmailAsync(toEmail, subject, body);
		}
		#endregion
	}
}