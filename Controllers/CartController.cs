using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ShopQuanAo.Models.BEAN.DTO;
using ShopQuanAo.BO;
using ShopQuanAo.Models.BEAN.Entity;

namespace ShopQuanAo.Controllers
{
    [Authorize]
    public class CartController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly CartService _cartService;

        public CartController(UserManager<ApplicationUser> userManager, CartService cartService)
        {
            _userManager = userManager;
            _cartService = cartService;
        }

        public async Task<IActionResult> Index()
        {
            var cart = await _cartService.GetCartAsync(_userManager.GetUserId(User));
            return View(cart);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToCart(int productId, int quantity, string size)
        {
            var result = await _cartService.AddToCartAsync(_userManager.GetUserId(User), new AddToCartDto { ProductId = productId, Quantity = quantity, Size = size });
            if (!result.Success) return BadRequest(result.Message);
            return Json(new { success = true, cartCount = result.CartCount });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateQuantity(int cartDetailId, int quantity)
        {
            var result = await _cartService.UpdateQuantityAsync(_userManager.GetUserId(User), new UpdateCartQtyDto { CartDetailId = cartDetailId, Quantity = quantity });
            if (!result.Success) return Json(new { success = false, message = result.Message, maxQty = (result.Data as dynamic)?.maxQty });
            return Json(new { success = true, data = result.Data });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveItem(int cartDetailId)
        {
            var data = await _cartService.RemoveItemAsync(_userManager.GetUserId(User), cartDetailId);
            return Json(new { success = true, data });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClearCart()
        {
            await _cartService.ClearCartAsync(_userManager.GetUserId(User));
            return Json(new { success = true });
        }
    }
}