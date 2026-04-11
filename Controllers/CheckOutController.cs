using Microsoft.AspNetCore.Authorization;
 using Microsoft.AspNetCore.Identity; using ShopQuanAo.Models;
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
		// SỬA TẠI ĐÂY: Thay ApplicationUser bằng ApplicationUser
		private readonly UserManager<ApplicationUser> _userManager;

		public CheckoutController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
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

			var cart = await _context.ShoppingCarts
				.Include(c => c.CartDetails)
					.ThenInclude(cd => cd.Product)
				.FirstOrDefaultAsync(c => c.UserId == userId && !c.IsDeleted);

			if (cart == null || cart.CartDetails == null || !cart.CartDetails.Any())
			{
				return RedirectToAction("Index", "Cart");
			}

			// Xóa các lỗi validate không cần thiết
			ModelState.Remove("UserId");
			ModelState.Remove("OrderStatus");
			ModelState.Remove("OrderDetails");
			ModelState.Remove("ApplicationUser"); // Thêm dòng này nếu Model Order có liên kết với User

			if (!ModelState.IsValid)
			{
				ViewBag.Cart = cart;
				ViewBag.TotalAmount = cart.CartDetails.Sum(cd => cd.UnitPrice * cd.Quantity);
				return View("Index", order);
			}

			// Gán thông tin đơn hàng
			order.UserId = userId ?? "";
			order.CreateTime = DateTime.Now;
			order.IsDeleted = false;
			order.IsPaid = false;
			order.OrderStatusId = 1;

			if (string.IsNullOrEmpty(order.PaymentMethod))
			{
				order.PaymentMethod = "COD"; // Mặc định là thanh toán khi nhận hàng
			}

			_context.Orders.Add(order);
			await _context.SaveChangesAsync();

			// Chuyển từ giỏ hàng sang chi tiết đơn hàng
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

			// Xóa giỏ hàng sau khi đặt thành công
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