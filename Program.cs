using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ShopQuanAo.Data;
using ShopQuanAo.Models;

var builder = WebApplication.CreateBuilder(args);

// 1. Cấu hình Connection String
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
	?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
	options.UseSqlServer(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// 2. Cấu hình Identity với ApplicationUser
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options => {
	// Yêu cầu xác thực tài khoản (OTP) mới được đăng nhập
	options.SignIn.RequireConfirmedAccount = true;

	// Cấu hình mật khẩu linh hoạt cho môi trường học tập
	options.Password.RequireDigit = false;
	options.Password.RequiredLength = 6;
	options.Password.RequireNonAlphanumeric = false;
	options.Password.RequireUppercase = false;
	options.Password.RequireLowercase = false;

	// Cấu hình khóa tài khoản nếu nhập sai nhiều lần (tùy chọn)
	options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
	options.Lockout.MaxFailedAccessAttempts = 5;
})
	.AddEntityFrameworkStores<ApplicationDbContext>()
	.AddDefaultUI()
	.AddDefaultTokenProviders();

// 3. Cấu hình Cookie (Quan trọng để điều hướng khi chưa xác thực)
builder.Services.ConfigureApplicationCookie(options => {
	options.LoginPath = $"/Identity/Account/Login";
	options.LogoutPath = $"/Identity/Account/Logout";
	options.AccessDeniedPath = $"/Identity/Account/AccessDenied";
});

builder.Services.AddControllersWithViews();

var app = builder.Build();

// 4. Khởi tạo dữ liệu mẫu (Admin, Roles...)
using (var scope = app.Services.CreateScope())
{
	var services = scope.ServiceProvider;
	// Đảm bảo file DataSeed đã được sửa UserManager<ApplicationUser>
	await DataSeed.KhoiTaoDuLieuMacDinh(services);
}

// Cấu hình Pipeline
if (app.Environment.IsDevelopment())
{
	app.UseMigrationsEndPoint();
}
else
{
	app.UseExceptionHandler("/Home/Error");
	app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication(); // Luôn đứng trước Authorization
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
	name: "default",
	pattern: "{controller=Home}/{action=Index}/{id?}")
	.WithStaticAssets();

app.MapRazorPages()
   .WithStaticAssets();

app.Run();