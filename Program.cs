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
// Chỉnh RequireConfirmedAccount = false để dễ test đồ án Quốc nhé
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options => {
	options.SignIn.RequireConfirmedAccount = false; // Chỉnh thành false ở đây

	options.Password.RequireDigit = false;
	options.Password.RequiredLength = 6;
	options.Password.RequireNonAlphanumeric = false;
	options.Password.RequireUppercase = false;
	options.Password.RequireLowercase = false;
})
	.AddEntityFrameworkStores<ApplicationDbContext>()
	.AddDefaultUI()
	.AddDefaultTokenProviders();

// 3. Cấu hình Cookie
builder.Services.ConfigureApplicationCookie(options => {
	options.LoginPath = $"/Identity/Account/Login";
	options.LogoutPath = $"/Identity/Account/Logout";
	options.AccessDeniedPath = $"/Identity/Account/AccessDenied";
});

builder.Services.AddControllersWithViews();

var app = builder.Build();

// 4. Khởi tạo dữ liệu mẫu
using (var scope = app.Services.CreateScope())
{
	var services = scope.ServiceProvider;
	try
	{
		await DataSeed.KhoiTaoDuLieuMacDinh(services);
	}
	catch (Exception ex)
	{
		// Tránh app bị sập nếu DataSeed có lỗi
		var logger = services.GetRequiredService<ILogger<Program>>();
		logger.LogError(ex, "Lỗi khi khởi tạo dữ liệu mẫu.");
	}
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

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
	name: "default",
	pattern: "{controller=Home}/{action=Index}/{id?}")
	.WithStaticAssets();

app.MapRazorPages()
   .WithStaticAssets();

app.Run();