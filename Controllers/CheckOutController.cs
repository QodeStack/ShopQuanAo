using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ShopQuanAo.Models.Entity;
using ShopQuanAo.Models.DTO;
using ShopQuanAo.Services;

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

        public async Task<IActionResult> Index()
        {
            var cart = await _checkoutService.GetCartForCheckoutAsync(_userManager.GetUserId(User));
            if (cart == null || cart.CartDetails == null || !cart.CartDetails.Any())
            {
                return RedirectToAction("Index", "Cart");
            }

            ViewBag.Cart = cart;
            ViewBag.TotalAmount = cart.CartDetails.Sum(cd => cd.UnitPrice * cd.Quantity);

            // FIX: Trả về PlaceOrderDto thay vì Order để đồng nhất dữ liệu
            return View(new PlaceOrderDto());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder(PlaceOrderDto dto)
        {
            if (!ModelState.IsValid)
            {
                var cart = await _checkoutService.GetCartForCheckoutAsync(_userManager.GetUserId(User));
                ViewBag.Cart = cart;
                ViewBag.TotalAmount = cart?.CartDetails?.Sum(cd => cd.UnitPrice * cd.Quantity) ?? 0;

                // Trả về cùng một View "Index" với dữ liệu DTO
                return View("Index", dto);
            }

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