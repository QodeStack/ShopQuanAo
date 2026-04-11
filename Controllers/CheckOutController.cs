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

        // Giao diện cbi thanh toán : xong 
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

        // Quy trình thanh toán : xong 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder(Order order)
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
            ModelState.Remove("UserId");
            ModelState.Remove("OrderStatus");
            ModelState.Remove("OrderDetails");
            if (!ModelState.IsValid)
            {
                ViewBag.Cart = cart; 
                ViewBag.TotalAmount = cart.CartDetails.Sum(cd => cd.UnitPrice * cd.Quantity);
                return View("Index", order);
            }
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
                    UnitPrice = (double)item.UnitPrice,
                    Size = item.Size
                };
                _context.OrderDetails.Add(orderDetail);
                var productSize = await _context.ProductSizes
                    .Include(ps => ps.Size)
                    .FirstOrDefaultAsync(ps => ps.ProductId == item.ProductId
                                            && ps.Size.SizeName == item.Size);

                if (productSize != null)
                {
                    productSize.Quantity -= item.Quantity;
                    if (productSize.Quantity < 0)
                        productSize.Quantity = 0;
                }
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