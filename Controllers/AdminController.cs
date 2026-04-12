using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopQuanAo.Data;
using ShopQuanAo.Models.Entity;
using ShopQuanAo.Services;
using ShopQuanAo.Models.DTO;

namespace ShopQuanAo.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AdminService _service;

        public AdminController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, AdminService service)
        {
            _context = context;
            _userManager = userManager;
            _service = service;
        }

        // --- Views ---
        public IActionResult Index()
        {
            ViewBag.RecentOrders = _context.Orders.Include(o => o.OrderDetails).OrderByDescending(o => o.CreateTime).Take(10).ToList();
            return View();
        }
        public IActionResult Users() => View();
        public IActionResult Products() { ViewBag.Categories = _context.Categories.ToList(); return View(); }
        public async Task<IActionResult> Contacts() => View(await _context.Contacts.OrderByDescending(c => c.CreatedDate).ToListAsync());
        public IActionResult Orders() => View();

        // --- API Endpoints ---
        [HttpGet] public async Task<IActionResult> GetStats() => Json(await _service.GetStatsAsync());
        [HttpGet] public async Task<IActionResult> GetUsers() => Json(await _service.GetUsersAsync());
        [HttpGet] public async Task<IActionResult> GetProducts() => Json(await _service.GetAllProductsAsync());
        [HttpGet] public async Task<IActionResult> GetOrders() => Json(await _service.GetOrdersAsync());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserDto dto)
        {
            var res = await _service.CreateUserAsync(dto);
            return Json(new { success = res.Success, message = res.Message });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateProduct([FromBody] CreateProductWithSizesDto dto)
        {
            var res = await _service.CreateProductAsync(dto);
            return Json(new { success = res.Success, message = res.Message });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateOrderStatus([FromBody] UpdateOrderStatusDto dto)
        {
            var res = await _service.UpdateOrderStatusAsync(dto);
            return Json(new { success = res.Success, newStatus = res.NewStatus });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser([FromBody] DeleteDto dto)
        {
            var success = await _service.DeleteUserAsync(dto.Id, _userManager.GetUserId(User));
            return Json(new { success, message = success ? "Xóa thành công" : "Lỗi!" });
        }
    }
}