using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopQuanAo.Data;
using ShopQuanAo.Models;

namespace ShopQuanAo.Controllers
{
    [Authorize]
    public class CheckoutController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public CheckoutController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);

            var cart = await _context.ShoppingCarts
                .Include(c => c.CartDetails)
                    .ThenInclude(cd => cd.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId && !c.IsDeleted);

            if (cart == null || cart.CartDetails == null || !cart.CartDetails.Any())
            {
                return RedirectToAction("Index", "Cart");
            }

            ViewBag.Cart = cart;
            ViewBag.TotalAmount = cart.CartDetails.Sum(cd => cd.UnitPrice * cd.Quantity);

            return View(new Order());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder(Order order)
        {
            var userId = _userManager.GetUserId(User);

            // Bổ sung .ThenInclude để lấy được Data của Product khi load lại View
            var cart = await _context.ShoppingCarts
                .Include(c => c.CartDetails)
                    .ThenInclude(cd => cd.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId && !c.IsDeleted);

            if (cart == null || cart.CartDetails == null || !cart.CartDetails.Any())
            {
                return RedirectToAction("Index", "Cart");
            }

            // Xóa các lỗi validate không cần thiết của object Order
            ModelState.Remove("UserId");
            ModelState.Remove("OrderStatus");
            ModelState.Remove("OrderDetails");

            // Nếu form nhập thiếu (họ tên, sđt, địa chỉ...), load lại trang Checkout
            if (!ModelState.IsValid)
            {
                ViewBag.Cart = cart; // Lúc này cart đã có đủ Product để View không bị lỗi Null
                ViewBag.TotalAmount = cart.CartDetails.Sum(cd => cd.UnitPrice * cd.Quantity);
                return View("Index", order);
            }

            // Set các thông tin mặc định cho đơn hàng
            order.UserId = userId;
            order.CreateTime = DateTime.Now;
            order.IsDeleted = false;
            order.IsPaid = false;
            order.OrderStatusId = 1;

            if (string.IsNullOrEmpty(order.PaymentMethod))
            {
                order.PaymentMethod = "MoMo"; 
            }

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            foreach (var item in cart.CartDetails)
            {
                var orderDetail = new OrderDetail
                {
                    OrderId = order.Id,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = (double)item.UnitPrice
                };
                _context.OrderDetails.Add(orderDetail);
            }

            _context.CartDetails.RemoveRange(cart.CartDetails);
            await _context.SaveChangesAsync();

            return RedirectToAction("OrderSuccess", new { orderId = order.Id });
        }

        public IActionResult OrderSuccess(int orderId)
        {
            ViewBag.OrderId = orderId;
            return View();
        }
    }
}