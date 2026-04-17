using Microsoft.AspNetCore.Identity;
using ShopQuanAo.Const;
using ShopQuanAo.Models.BEAN.Entity;

namespace ShopQuanAo.Data
{
	public class DataSeed
	{
		public static async Task KhoiTaoDuLieuMacDinh(IServiceProvider dichVu)
		{
			// SỬA TẠI ĐÂY: Thay ApplicationUser bằng ApplicationUser
			var QuanLyNguoiDung = dichVu.GetRequiredService<UserManager<ApplicationUser>>();
			var QuanLyVaiTro = dichVu.GetRequiredService<RoleManager<IdentityRole>>();

			// Thêm các vai trò vào cơ sở dữ liệu nếu chưa tồn tại 
			if (!await QuanLyVaiTro.RoleExistsAsync(Roles.Admin.ToString()))
			{
				await QuanLyVaiTro.CreateAsync(new IdentityRole(Roles.Admin.ToString()));
			}
			if (!await QuanLyVaiTro.RoleExistsAsync(Roles.User.ToString()))
			{
				await QuanLyVaiTro.CreateAsync(new IdentityRole(Roles.User.ToString()));
			}

			// Tạo thông tin mặc định cho tài khoản admin bằng ApplicationUser
			var quanTri = new ApplicationUser
			{
				UserName = "admin@gmail.com",
				Email = "admin@gmail.com",
				EmailConfirmed = true,
				// Khởi tạo các cột mới để tránh lỗi null nếu cần
				OTPCode = null,
				OTPExpiry = null
			};

			var nguoiDungTrongCSDL = await QuanLyNguoiDung.FindByEmailAsync(quanTri.Email);

			if (nguoiDungTrongCSDL is null)
			{
				// Tạo tài khoản admin với mật khẩu mặc định là Admin@123
				var ketQua = await QuanLyNguoiDung.CreateAsync(quanTri, "Admin@123");

				if (ketQua.Succeeded)
				{
					// Gán vai trò Admin cho tài khoản này
					await QuanLyNguoiDung.AddToRoleAsync(quanTri, Roles.Admin.ToString());
				}
				else
				{
					foreach (var loi in ketQua.Errors)
					{
						Console.WriteLine(loi.Description);
					}
				}
			}
		}
	}
}