using Microsoft.AspNetCore.Authorization;
 using Microsoft.AspNetCore.Identity; using ShopQuanAo.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopQuanAo.Data;
using ShopQuanAo.ViewModels;

namespace ShopQuanAo.Controllers
{
    [Authorize]
    public class CustomerController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CustomerController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(string status = "", int page = 1)
        {
            const int pageSize = 5;
            var userId = _userManager.GetUserId(User);

            var query = _context.Orders
                .Include(o => o.OrderStatus)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .Where(o => o.UserId == userId && !o.IsDeleted)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(o => o.OrderStatus.StatusName == status);

            query = query.OrderByDescending(o => o.CreateTime);

            var total = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(total / (double)pageSize);
            page = Math.Clamp(page, 1, Math.Max(1, totalPages));

            var orders = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var vm = new CustomerOrderViewModel
            {
                Orders = orders,
                CurrentPage = page,
                TotalPages = totalPages,
                CurrentStatus = status
            };

            return View(vm);
        }

        public async Task<IActionResult> Detail(int id)
        {
            var userId = _userManager.GetUserId(User);
            var order = await _context.Orders
                .Include(o => o.OrderStatus)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId && !o.IsDeleted);

            if (order == null) return NotFound();
            return View(order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int orderId)
        {
            var userId = _userManager.GetUserId(User);
            var order = await _context.Orders
                .Include(o => o.OrderStatus)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);

            if (order == null) return NotFound();

            if (order.OrderStatus?.StatusName == "Chờ xử lý")
            {
                var cancelStatus = await _context.OrderStatuses
                    .FirstOrDefaultAsync(s => s.StatusName == "Đã hủy");
                if (cancelStatus != null)
                {
                    order.OrderStatusId = cancelStatus.Id;
                    await _context.SaveChangesAsync();
                    TempData["Message"] = "Đã hủy đơn hàng thành công.";
                }
            }

            return RedirectToAction(nameof(Index));
        }
    }
}