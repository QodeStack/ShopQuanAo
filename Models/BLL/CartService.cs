using Microsoft.EntityFrameworkCore;
using ShopQuanAo.Data;
using ShopQuanAo.Models.Entity;
using ShopQuanAo.Models.DTO;

namespace ShopQuanAo.Services
{
    public class CartService
    {
        private readonly ApplicationDbContext _context;

        public CartService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ShoppingCart> GetCartAsync(string userId)
        {
            var cart = await _context.ShoppingCarts
                .Include(c => c.CartDetails).ThenInclude(cd => cd.Product)
                .Include(c => c.CartDetails).ThenInclude(cd => cd.Product).ThenInclude(p => p.ProductSizes).ThenInclude(ps => ps.Size)
                .FirstOrDefaultAsync(c => c.UserId == userId && !c.IsDeleted);

            return cart ?? new ShoppingCart { UserId = userId, CartDetails = new List<CartDetail>() };
        }

        public async Task<(bool Success, string Message, int CartCount)> AddToCartAsync(string userId, AddToCartDto dto)
        {
            var product = await _context.Products.FindAsync(dto.ProductId);
            if (product == null) return (false, "Sản phẩm không tồn tại.", 0);

            var productSize = await _context.ProductSizes
                .Include(ps => ps.Size)
                .FirstOrDefaultAsync(ps => ps.ProductId == dto.ProductId && ps.Size != null && ps.Size.SizeName == dto.Size);

            if (productSize == null) return (false, $"Sản phẩm không có size {dto.Size}.", 0);

            var cart = await _context.ShoppingCarts
                .Include(c => c.CartDetails)
                .FirstOrDefaultAsync(c => c.UserId == userId && !c.IsDeleted);

            if (cart == null)
            {
                cart = new ShoppingCart { UserId = userId };
                _context.ShoppingCarts.Add(cart);
                await _context.SaveChangesAsync();
            }

            var existingDetail = cart.CartDetails?.FirstOrDefault(cd => cd.ProductId == dto.ProductId && cd.Size == dto.Size);
            int currentQty = existingDetail?.Quantity ?? 0;
            int requestedTotal = currentQty + dto.Quantity;

            if (requestedTotal > productSize.Quantity)
                return (false, $"Chỉ còn {productSize.Quantity - currentQty} sản phẩm size {dto.Size} có thể thêm.", 0);

            if (existingDetail != null) existingDetail.Quantity += dto.Quantity;
            else
            {
                _context.CartDetails.Add(new CartDetail
                {
                    ShoppingCartId = cart.Id,
                    ProductId = dto.ProductId,
                    Quantity = dto.Quantity,
                    UnitPrice = product.Price,
                    Size = dto.Size
                });
            }

            await _context.SaveChangesAsync();
            int cartCount = await _context.CartDetails.Where(cd => cd.ShoppingCart.UserId == userId && !cd.ShoppingCart.IsDeleted).SumAsync(cd => cd.Quantity);
            return (true, "", cartCount);
        }

        public async Task<(bool Success, string Message, object Data)> UpdateQuantityAsync(string userId, UpdateCartQtyDto dto)
        {
            var cartDetail = await _context.CartDetails
                .Include(cd => cd.ShoppingCart).Include(cd => cd.Product)
                .FirstOrDefaultAsync(cd => cd.Id == dto.CartDetailId && cd.ShoppingCart.UserId == userId);

            if (cartDetail == null) return (false, "Không tìm thấy sản phẩm trong giỏ.", null!);

            var productSize = await _context.ProductSizes.Include(ps => ps.Size)
                .FirstOrDefaultAsync(ps => ps.ProductId == cartDetail.ProductId && ps.Size.SizeName == cartDetail.Size);

            int maxQty = productSize?.Quantity ?? 0;
            if (dto.Quantity > maxQty) return (false, $"Chỉ còn {maxQty} sản phẩm trong kho.", new { maxQty });

            if (dto.Quantity <= 0) _context.CartDetails.Remove(cartDetail);
            else cartDetail.Quantity = dto.Quantity;

            await _context.SaveChangesAsync();

            var cart = await GetCartAsync(userId);
            return (true, "", new
            {
                subtotal = cartDetail.UnitPrice * dto.Quantity,
                total = cart.CartDetails.Sum(cd => cd.UnitPrice * cd.Quantity),
                cartCount = cart.CartDetails.Sum(cd => cd.Quantity),
                maxQty
            });
        }

        public async Task<object> RemoveItemAsync(string userId, int cartDetailId)
        {
            var cartDetail = await _context.CartDetails
                .FirstOrDefaultAsync(cd => cd.Id == cartDetailId && cd.ShoppingCart.UserId == userId);

            if (cartDetail != null)
            {
                _context.CartDetails.Remove(cartDetail);
                await _context.SaveChangesAsync();
            }

            var cart = await GetCartAsync(userId);
            return new
            {
                total = cart.CartDetails.Sum(cd => cd.UnitPrice * cd.Quantity),
                cartCount = cart.CartDetails.Sum(cd => cd.Quantity)
            };
        }

        public async Task ClearCartAsync(string userId)
        {
            var cart = await _context.ShoppingCarts.Include(c => c.CartDetails)
                .FirstOrDefaultAsync(c => c.UserId == userId && !c.IsDeleted);

            if (cart?.CartDetails != null)
            {
                _context.CartDetails.RemoveRange(cart.CartDetails);
                await _context.SaveChangesAsync();
            }
        }
    }
}