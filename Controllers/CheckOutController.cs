using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ShopQuanAo.BO;
using ShopQuanAo.Models.BEAN.DTO;
using ShopQuanAo.Models.BEAN.Entity;

namespace ShopQuanAo.Controllers
{
    [Authorize]
    public class CheckoutController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly CheckoutService _checkoutService;

        public CheckoutController(UserManager<ApplicationUser> userManager, CheckoutService checkoutService)
        {
            _userManager = userManager;
            _checkoutService = checkoutService;
        }

        // ĐÃ SỬA: Hứng selectedIds từ thanh URL
        [HttpGet]
        public async Task<IActionResult> Index([FromQuery] List<int> selectedIds)
        {
            var userId = _userManager.GetUserId(User);
            var cart = await _checkoutService.GetCartForCheckoutAsync(userId, selectedIds);

            if (cart == null || cart.CartDetails == null || !cart.CartDetails.Any())
            {
                return RedirectToAction("Index", "Cart");
            }

            ViewBag.Cart = cart;
            ViewBag.TotalAmount = cart.CartDetails.Sum(cd => cd.UnitPrice * cd.Quantity);

            // Khởi tạo DTO và nhét sẵn danh sách ID
            var dto = new PlaceOrderDto { SelectedIds = selectedIds };

            // TÍNH NĂNG MỚI: Tự động điền thông tin từ đơn hàng cũ (nếu có)
            var lastOrder = await _checkoutService.GetLatestOrderAsync(userId);
            if (lastOrder != null)
            {
                dto.Name = lastOrder.Name;
                dto.MobileNumber = lastOrder.MobileNumber;
                dto.Address = lastOrder.Address;
                dto.Email = lastOrder.Email; // Thêm dòng này nếu Order của bạn có lưu Email
            }

            // Truyền dto đã có sẵn data sang View
            return View(dto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder(PlaceOrderDto dto)
        {
            if (!ModelState.IsValid)
            {
                // ĐÃ SỬA: Truyền dto.SelectedIds vào để load lại đúng giỏ hàng nếu nhập sai form
                var cart = await _checkoutService.GetCartForCheckoutAsync(_userManager.GetUserId(User), dto.SelectedIds);
                ViewBag.Cart = cart;
                ViewBag.TotalAmount = cart?.CartDetails?.Sum(cd => cd.UnitPrice * cd.Quantity) ?? 0;

                return View("Index", dto);
            }

            // ĐÃ SỬA: Gọi hàm đã được dọn dẹp bên Service
            var result = await _checkoutService.PlaceOrderAsync(_userManager.GetUserId(User), dto);
            if (!result.Success) return RedirectToAction("Index", "Cart");

            return RedirectToAction("OrderSuccess", new { orderId = result.OrderId });
        }

        public IActionResult OrderSuccess(int orderId)
        {
            ViewBag.OrderId = orderId;
            return View();
        }
    }
}