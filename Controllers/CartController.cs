//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Identity;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;
//using ShopQuanAo.Data;
//using ShopQuanAo.Models;

//public class CartController : Controller
//{
//    private readonly ApplicationDbContext _db;
//    private readonly UserManager<IdentityUser> _userManager;

//    public CartController(ApplicationDbContext db, UserManager<IdentityUser> userManager)
//    {
//        _db = db;
//        _userManager = userManager;
//    }

//    public IActionResult Index()
//    {

//        return View();
//    }

//    [HttpPost]
//    [ValidateAntiForgeryToken]
//    public async Task<IActionResult> AddToCart(int productId, string size, int quantity = 1)
//    {
//        // Check authentication first
//        if (!User.Identity.IsAuthenticated)
//            return Unauthorized();

//        var userId = _userManager.GetUserId(User);
//        if (string.IsNullOrEmpty(userId))
//            return Unauthorized();

//        // Validate input
//        if (string.IsNullOrWhiteSpace(size))
//            return BadRequest("Vui lòng chọn kích cỡ");

//        if (quantity <= 0)
//            return BadRequest("Số lượng phải lớn hơn 0");

//        // Check if product exists and has stock for the given size
//        var productSize = await _db.ProductSizes
//            .Include(ps => ps.Product)
//            .Include(ps => ps.Size)
//            .FirstOrDefaultAsync(ps => ps.ProductId == productId && ps.Size.SizeName == size);

//        if (productSize == null || productSize.Product == null)
//            return NotFound("Sản phẩm không tồn tại");

//        if (productSize.Quantity < quantity)
//            return BadRequest($"Không đủ hàng trong kho. Còn lại: {productSize.Quantity}");

//        // Get or create shopping cart
//        var cart = await _db.ShoppingCarts
//            .FirstOrDefaultAsync(c => c.UserId == userId && !c.IsDeleted);

//        if (cart == null)
//        {
//            cart = new ShoppingCart { UserId = userId };
//            _db.ShoppingCarts.Add(cart);
//            await _db.SaveChangesAsync();
//        }

//        // Check if item with same product and size already exists
//        var existing = await _db.CartDetails
//            .FirstOrDefaultAsync(d => d.ShoppingCartId == cart.Id && d.ProductId == productId && d.Size == size);

//        if (existing != null)
//        {
//            // Check if total quantity won't exceed available stock
//            if (existing.Quantity + quantity > productSize.Quantity)
//                return BadRequest($"Không đủ hàng trong kho. Còn lại: {productSize.Quantity}, Đã có trong giỏ: {existing.Quantity}");

//            existing.Quantity += quantity;
//        }
//        else
//        {
//            _db.CartDetails.Add(new CartDetail
//            {
//                ShoppingCartId = cart.Id,
//                ProductId = productId,
//                Size = size,
//                Quantity = quantity,
//                UnitPrice = productSize.Product.Price
//            });
//        }

//        await _db.SaveChangesAsync();
//        return Ok("Thêm vào giỏ hàng thành công");
//    }
//}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
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
        private readonly UserManager<IdentityUser> _userManager;

        public CartController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
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
                .FirstOrDefaultAsync(c => c.UserId == userId && !c.IsDeleted);

            if (cart == null)
            {
                cart = new ShoppingCart
                {
                    UserId = userId,
                    CartDetails = new List<CartDetail>()
                };
            }

            return View(cart);
        }

        // POST: /Cart/AddToCart
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToCart(int productId, int quantity, string size)
        {
            var userId = _userManager.GetUserId(User);

            var product = await _context.Products.FindAsync(productId);
            if (product == null)
                return Json(new { success = false, message = "Sản phẩm không tồn tại." });

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

            if (existingDetail != null)
            {
                existingDetail.Quantity += quantity;
            }
            else
            {
                var cartDetail = new CartDetail
                {
                    ShoppingCartId = cart.Id,
                    ProductId = productId,
                    Quantity = quantity,
                    UnitPrice = product.Price,
                    Size = size
                };
                _context.CartDetails.Add(cartDetail);
            }

            await _context.SaveChangesAsync();

            var cartCount = await _context.CartDetails
                .Where(cd => cd.ShoppingCart.UserId == userId && !cd.ShoppingCart.IsDeleted)
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
                .FirstOrDefaultAsync(cd => cd.Id == cartDetailId && cd.ShoppingCart.UserId == userId);

            if (cartDetail == null)
                return Json(new { success = false, message = "Không tìm thấy sản phẩm trong giỏ." });

            if (quantity <= 0)
            {
                _context.CartDetails.Remove(cartDetail);
            }
            else
            {
                cartDetail.Quantity = quantity;
            }

            await _context.SaveChangesAsync();

            var cart = await _context.ShoppingCarts
                .Include(c => c.CartDetails)
                .FirstOrDefaultAsync(c => c.UserId == userId && !c.IsDeleted);

            var subtotal = cartDetail.UnitPrice * quantity;
            var total = cart?.CartDetails?.Sum(cd => cd.UnitPrice * cd.Quantity) ?? 0;
            var cartCount = cart?.CartDetails?.Sum(cd => cd.Quantity) ?? 0;

            return Json(new { success = true, subtotal, total, cartCount });
        }

        // POST: /Cart/RemoveItem
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveItem(int cartDetailId)
        {
            var userId = _userManager.GetUserId(User);

            var cartDetail = await _context.CartDetails
                .Include(cd => cd.ShoppingCart)
                .FirstOrDefaultAsync(cd => cd.Id == cartDetailId && cd.ShoppingCart.UserId == userId);

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

        // GET: /Cart/Count (for navbar badge)
        [HttpGet]
        public async Task<IActionResult> Count()
        {
            var userId = _userManager.GetUserId(User);
            var count = await _context.CartDetails
                .Where(cd => cd.ShoppingCart.UserId == userId && !cd.ShoppingCart.IsDeleted)
                .SumAsync(cd => cd.Quantity);

            return Json(new { count });
        }
    }
}