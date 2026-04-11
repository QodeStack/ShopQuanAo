using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
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
        private readonly UserManager<IdentityUser> _userManager;

        public CustomerController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // giao diên chính : xong 
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

        // Hủy đơn : xong 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int orderId)
        {
            var userId = _userManager.GetUserId(User);
            var order = await _context.Orders
                .Include(o => o.OrderStatus)
                .Include(o => o.OrderDetails) 
                .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);

            if (order == null) return NotFound();

            if (order.OrderStatus?.StatusName == "Chờ xác nhận")
            {
                var cancelStatus = await _context.OrderStatuses
                    .FirstOrDefaultAsync(s => s.StatusName == "Đã hủy");

                if (cancelStatus != null)
                {
                    order.OrderStatusId = cancelStatus.Id;
                    foreach (var detail in order.OrderDetails)
                    {
                        var productSize = await _context.ProductSizes
                            .Include(ps => ps.Size)
                            .FirstOrDefaultAsync(ps => ps.ProductId == detail.ProductId
                                                    && ps.Size.SizeName == detail.Size);

                        if (productSize != null)
                            productSize.Quantity += detail.Quantity;
                    }

                    await _context.SaveChangesAsync();
                    TempData["Message"] = "Đã hủy đơn hàng thành công.";
                }
            }

            return RedirectToAction(nameof(Index));
        }

        // Xác nhận đã nhận hàng : xong 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmReceived(int orderId)
        {
            var userId = _userManager.GetUserId(User);
            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);

            if (order == null) return NotFound();
            var completedStatus = await _context.OrderStatuses
                .FirstOrDefaultAsync(s => s.StatusName == "Đã hoàn thành");

            if (completedStatus != null)
            {
                order.OrderStatusId = completedStatus.Id;
                order.OrderStatus = null;
                order.IsPaid = true;
                _context.Entry(order).State = EntityState.Modified;
                await _context.SaveChangesAsync();
                TempData["Message"] = "Cảm ơn bạn đã mua hàng!";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}