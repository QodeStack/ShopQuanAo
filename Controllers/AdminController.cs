using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

        // ── Dashboard Stats API ────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> GetStats()
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

            var products = await _context.Products.CountAsync();

            return Json(new { revenue, orders, users, products });
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

            var user = new IdentityUser
            {
                UserName = dto.Email,
                Email = dto.Email,
                EmailConfirmed = true
            };

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

            // Update role
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
            return Json(result.Succeeded
                ? new { success = true }
                : new { success = false, message = "Lỗi khi xóa." });
        }

        // Trong action GetProducts, thêm Include ProductSizes và Size:
        [HttpGet]
        public async Task<IActionResult> GetProducts()
        {
            var products = await _context.Products
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
                    categoryName = p.Category.CategoryName,
                    productSizes = p.ProductSizes.Select(ps => new {
                        sizeName = ps.Size.SizeName,
                        quantity = ps.Quantity
                    }).ToList()
                })
                .ToListAsync();

            return Json(products);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateProduct([FromBody] ProductDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.ProductName))
                return Json(new { success = false, message = "Tên sản phẩm không được để trống." });

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
            return Json(new { success = true, id = product.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProduct([FromBody] ProductDto dto)
        {
            var product = await _context.Products.FindAsync(dto.Id);
            if (product == null) return Json(new { success = false, message = "Không tìm thấy sản phẩm." });

            product.ProductName = dto.ProductName;
            product.BrandName = dto.BrandName ?? product.BrandName;
            product.Price = dto.Price;
            product.Image = dto.Image;
            product.CategoryId = dto.CategoryId;

            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProduct([FromBody] DeleteIntDto dto)
        {
            var product = await _context.Products.FindAsync(dto.Id);
            if (product == null) return Json(new { success = false, message = "Không tìm thấy sản phẩm." });

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }
    }

    // ── DTOs ───────────────────────────────────────────────────
    public class CreateUserDto { public string Email { get; set; } public string Password { get; set; } public string Role { get; set; } }
    public class EditUserDto { public string Id { get; set; } public string Role { get; set; } }
    public class DeleteDto { public string Id { get; set; } }
    public class DeleteIntDto { public int Id { get; set; } }
    public class ProductDto { public int Id { get; set; } public string ProductName { get; set; } public string BrandName { get; set; } public double Price { get; set; } public string Image { get; set; } public int CategoryId { get; set; } }
}
