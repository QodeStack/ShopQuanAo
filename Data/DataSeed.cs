using Microsoft.AspNetCore.Identity;
using ShopQuanAo.Const;
namespace ShopQuanAo.Data
{
    public class DataSeed
    {
        public static async Task KhoiTaoDuLieuMacDinh(IServiceProvider dichVu) { 
            var QuanLyNguoiDung = dichVu.GetService<UserManager<IdentityUser>>();
            var QuanLyVaiTro = dichVu.GetService<RoleManager<IdentityRole>>();

            // thêm 1 vai trò vào cơ sở dữ liệu nếu nó chưa tồn tại 
            await QuanLyVaiTro.CreateAsync(new IdentityRole(Roles.Admin.ToString()));
            await QuanLyVaiTro.CreateAsync(new IdentityRole(Roles.User.ToString()));

            // Tạo thông tin mặc định cho tài khoản admin nếu nó chưa tồn tại
            // bao gồm : UserName , Email , Password, Role
            var quanTri = new IdentityUser
            {
                UserName = "admin@gmail.com",
                Email = "admin@gmail.com",
                EmailConfirmed = true,
            };
            var nguoiDungTrongCSDL = await QuanLyNguoiDung.FindByEmailAsync(quanTri.Email);

            // Nếu tài khoản admin không tồn tại trong cơ sở dữ liệu
            // hay có thể hiểu là chưa có trong csdl
            if (nguoiDungTrongCSDL is null)
            {
                // tạo tài khoản admin với mật khẩu mặc định là Admin@123
                var ketQua = await QuanLyNguoiDung.CreateAsync(quanTri, "Admin@123");
                
                // Nếu tạo tài khoản admin thành công thì gán vai trò Admin cho tài khoản này
                if (ketQua.Succeeded)
                {
                    await QuanLyNguoiDung.AddToRoleAsync(quanTri, Roles.Admin.ToString());
                }
                else
                {
                    // in ra mã lỗi 
                    foreach (var loi in ketQua.Errors)
                    {
                        Console.WriteLine(loi.Description);
                    }
                }
            }
        }
    }
}
