using Microsoft.AspNetCore.Authorization;
 using Microsoft.AspNetCore.Identity; using ShopQuanAo.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopQuanAo.Data;
using ShopQuanAo.Models;

namespace ShopQuanAo.Controllers
{
	[Authorize]
	public class CartController : Controller
	{
		private readonly ApplicationDbContext _context;
		// SỬA TẠI ĐÂY: Thay ApplicationUser bằng ApplicationUser
		private readonly UserManager<ApplicationUser> _userManager;

		public CartController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
		{
			_context = context;
			_userManager = userManager;
		}

		// GET: /Cart
		public async Task<IActionResult> Index()
		{
			var userId = _userManager.GetUserId(User);

			var cart = await _context.ShoppingCarts
				.Include(c => c.CartDetails)
					.ThenInclude(cd => cd.Product)
						.ThenInclude(p => p.ProductSizes)
							.ThenInclude(ps => ps.Size)
				.FirstOrDefaultAsync(c => c.UserId == userId && !c.IsDeleted);

			if (cart == null)
			{
				cart = new ShoppingCart { UserId = userId ?? "", CartDetails = new List<CartDetail>() };
			}

			return View(cart);
		}

		// POST: /Cart/AddToCart
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> AddToCart(int productId, int quantity, string size)
		{
			var userId = _userManager.GetUserId(User);
			if (string.IsNullOrEmpty(userId)) return Unauthorized();

			var product = await _context.Products.FindAsync(productId);
			if (product == null)
				return BadRequest("Sản phẩm không tồn tại.");

			// Kiểm tra tồn kho theo size
			var productSize = await _context.ProductSizes
				.Include(ps => ps.Size)
				.FirstOrDefaultAsync(ps => ps.ProductId == productId && ps.Size != null && ps.Size.SizeName == size);

			if (productSize == null)
				return BadRequest($"Sản phẩm không có size {size}.");

			var cart = await _context.ShoppingCarts
				.Include(c => c.CartDetails)
				.FirstOrDefaultAsync(c => c.UserId == userId && !c.IsDeleted);

			if (cart == null)
			{
				cart = new ShoppingCart { UserId = userId };
				_context.ShoppingCarts.Add(cart);
				await _context.SaveChangesAsync();
			}

			var existingDetail = cart.CartDetails?
				.FirstOrDefault(cd => cd.ProductId == productId && cd.Size == size);

			int currentQtyInCart = existingDetail?.Quantity ?? 0;
			int requestedTotal = currentQtyInCart + quantity;

			if (requestedTotal > productSize.Quantity)
			{
				int available = productSize.Quantity - currentQtyInCart;
				if (available <= 0)
					return BadRequest($"Sản phẩm size {size} đã đủ số lượng trong giỏ hàng (tối đa {productSize.Quantity}).");
				return BadRequest($"Chỉ còn {available} sản phẩm size {size} có thể thêm vào giỏ.");
			}

			if (existingDetail != null)
			{
				existingDetail.Quantity += quantity;
			}
			else
			{
				_context.CartDetails.Add(new CartDetail
				{
					ShoppingCartId = cart.Id,
					ProductId = productId,
					Quantity = quantity,
					UnitPrice = product.Price,
					Size = size
				});
			}

			await _context.SaveChangesAsync();

			var cartCount = await _context.CartDetails
				.Where(cd => cd.ShoppingCart != null && cd.ShoppingCart.UserId == userId && !cd.ShoppingCart.IsDeleted)
				.SumAsync(cd => cd.Quantity);

			return Json(new { success = true, cartCount });
		}

		// POST: /Cart/UpdateQuantity
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> UpdateQuantity(int cartDetailId, int quantity)
		{
			var userId = _userManager.GetUserId(User);

			var cartDetail = await _context.CartDetails
				.Include(cd => cd.ShoppingCart)
				.Include(cd => cd.Product)
				.FirstOrDefaultAsync(cd => cd.Id == cartDetailId && cd.ShoppingCart != null && cd.ShoppingCart.UserId == userId);

			if (cartDetail == null)
				return Json(new { success = false, message = "Không tìm thấy sản phẩm trong giỏ." });

			// Kiểm tra tồn kho
			var productSize = await _context.ProductSizes
				.Include(ps => ps.Size)
				.FirstOrDefaultAsync(ps => ps.ProductId == cartDetail.ProductId
										&& ps.Size != null && ps.Size.SizeName == cartDetail.Size);

			int maxQty = productSize?.Quantity ?? 0;

			if (quantity > maxQty)
				return Json(new { success = false, message = $"Chỉ còn {maxQty} sản phẩm trong kho.", maxQty });

			if (quantity <= 0)
				_context.CartDetails.Remove(cartDetail);
			else
				cartDetail.Quantity = quantity;

			await _context.SaveChangesAsync();

			var cart = await _context.ShoppingCarts
				.Include(c => c.CartDetails)
				.FirstOrDefaultAsync(c => c.UserId == userId && !c.IsDeleted);

			var subtotal = cartDetail.UnitPrice * quantity;
			var total = cart?.CartDetails?.Sum(cd => cd.UnitPrice * cd.Quantity) ?? 0;
			var cartCount = cart?.CartDetails?.Sum(cd => cd.Quantity) ?? 0;

			return Json(new { success = true, subtotal, total, cartCount, maxQty });
		}

		// POST: /Cart/RemoveItem
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> RemoveItem(int cartDetailId)
		{
			var userId = _userManager.GetUserId(User);

			var cartDetail = await _context.CartDetails
				.Include(cd => cd.ShoppingCart)
				.FirstOrDefaultAsync(cd => cd.Id == cartDetailId && cd.ShoppingCart != null && cd.ShoppingCart.UserId == userId);

			if (cartDetail == null)
				return Json(new { success = false, message = "Không tìm thấy sản phẩm." });

			_context.CartDetails.Remove(cartDetail);
			await _context.SaveChangesAsync();

			var cart = await _context.ShoppingCarts
				.Include(c => c.CartDetails)
				.FirstOrDefaultAsync(c => c.UserId == userId && !c.IsDeleted);

			var total = cart?.CartDetails?.Sum(cd => cd.UnitPrice * cd.Quantity) ?? 0;
			var cartCount = cart?.CartDetails?.Sum(cd => cd.Quantity) ?? 0;

			return Json(new { success = true, total, cartCount });
		}

		// POST: /Cart/ClearCart
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> ClearCart()
		{
			var userId = _userManager.GetUserId(User);

			var cart = await _context.ShoppingCarts
				.Include(c => c.CartDetails)
				.FirstOrDefaultAsync(c => c.UserId == userId && !c.IsDeleted);

			if (cart != null && cart.CartDetails != null)
			{
				_context.CartDetails.RemoveRange(cart.CartDetails);
				await _context.SaveChangesAsync();
			}

			return Json(new { success = true });
		}
	}
}