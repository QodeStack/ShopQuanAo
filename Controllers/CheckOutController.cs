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
            // Đổ danh sách mã đang hoạt động ra ViewBag để hiển thị trên Popup
            ViewBag.Vouchers = await _checkoutService.GetActiveVouchersAsync();

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
        [HttpPost]
        public async Task<IActionResult> ApplyVoucher([FromBody] ApplyVoucherDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Code))
                return Json(new { success = false, message = "Vui lòng nhập mã khuyến mãi." });

            // Gọi DAO để lấy mã
            var voucher = await _checkoutService.GetVoucherByCodeAsync(dto.Code); // Nhớ viết thêm hàm trung gian này trong Service nhé

            if (voucher == null)
                return Json(new { success = false, message = "Mã khuyến mãi không tồn tại." });

            if (!voucher.IsActive)
                return Json(new { success = false, message = "Mã khuyến mãi này đã bị vô hiệu hóa." });

            if (voucher.Quantity <= 0)
                return Json(new { success = false, message = "Mã khuyến mãi đã hết lượt sử dụng." });

            if (dto.OrderTotal < voucher.MinOrderAmount)
                return Json(new { success = false, message = $"Đơn hàng tối thiểu phải từ {voucher.MinOrderAmount.ToString("N0")}đ để áp mã." });

            return Json(new
            {
                success = true,
                message = "Áp dụng mã thành công!",
                discountAmount = voucher.DiscountAmount,
                voucherCode = voucher.Code // Trả về mã chuẩn hóa để giao diện lưu lại
            });
        }

        public IActionResult OrderSuccess(int orderId)
        {
            ViewBag.OrderId = orderId;
            return View();
        }
    }
}