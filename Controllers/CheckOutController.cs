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
            ViewBag.Vouchers = await _checkoutService.GetActiveVouchersAsync();

            var dto = new PlaceOrderDto { SelectedIds = selectedIds };

            var lastOrder = await _checkoutService.GetLatestOrderAsync(userId);
            if (lastOrder != null)
            {
                dto.Name = lastOrder.Name;
                dto.MobileNumber = lastOrder.MobileNumber;
                dto.Address = lastOrder.Address;
                dto.Email = lastOrder.Email;
            }

            return View(dto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder(PlaceOrderDto dto)
        {
            if (!ModelState.IsValid)
            {
                var cart = await _checkoutService.GetCartForCheckoutAsync(_userManager.GetUserId(User), dto.SelectedIds);
                ViewBag.Cart = cart;
                ViewBag.TotalAmount = cart?.CartDetails?.Sum(cd => cd.UnitPrice * cd.Quantity) ?? 0;

                return View("Index", dto);
            }

            var result = await _checkoutService.PlaceOrderAsync(_userManager.GetUserId(User), dto);
            if (!result.Success) return RedirectToAction("Index", "Cart");

            // KIỂM TRA PHƯƠNG THỨC THANH TOÁN ĐỂ CHUYỂN HƯỚNG
            if (dto.PaymentMethod == "ChuyenKhoan")
            {
                return RedirectToAction("Payment", new { orderId = result.OrderId });
            }

            return RedirectToAction("OrderSuccess", new { orderId = result.OrderId });
        }

        [HttpPost]
        public async Task<IActionResult> ApplyVoucher([FromBody] ApplyVoucherDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Code))
                return Json(new { success = false, message = "Vui lòng nhập mã khuyến mãi." });

            var voucher = await _checkoutService.GetVoucherByCodeAsync(dto.Code);

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
                discountAmount = Math.Min(dto.OrderTotal, voucher.DiscountAmount),
                voucherCode = voucher.Code
            });
        }

        // TẠO GIAO DIỆN QUÉT MÃ QR RIÊNG BẰNG HÀM NÀY
        [HttpGet]
        public async Task<IActionResult> Payment(int orderId)
        {
            var userId = _userManager.GetUserId(User);
            var order = await _checkoutService.GetLatestOrderAsync(userId);

            if (order == null || order.Id != orderId) return RedirectToAction("Index", "Home");

            // Nếu đơn đã thanh toán rồi (ấn f5 lại) thì cho sang trang Success luôn
            if (order.IsPaid) return RedirectToAction("OrderSuccess", new { orderId = order.Id });

            ViewBag.OrderId = order.Id;
            ViewBag.TotalAmount = order.TotalAmount;
            ViewBag.CreateTime = order.CreateTime;
            return View();
        }

        // API TRẢ VỀ TRẠNG THÁI THANH TOÁN CHO JAVASCRIPT HỎI THĂM
        [HttpGet]
        public async Task<IActionResult> CheckPaymentStatus(int orderId)
        {
            var userId = _userManager.GetUserId(User);
            var order = await _checkoutService.GetLatestOrderAsync(userId);

            if (order != null && order.Id == orderId)
            {
                return Json(new { isPaid = order.IsPaid });
            }
            return Json(new { isPaid = false });
        }

        // TRANG THÀNH CÔNG THUẦN TÚY (LỜI CẢM ƠN)
        [HttpGet]
        public async Task<IActionResult> OrderSuccess(int orderId)
        {
            var userId = _userManager.GetUserId(User);
            var order = await _checkoutService.GetLatestOrderAsync(userId);

            if (order != null && order.Id == orderId)
            {
                ViewBag.OrderId = order.Id;
            }
            else
            {
                ViewBag.OrderId = orderId;
            }

            return View();
        }
    }
}