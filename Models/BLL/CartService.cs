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

        // 1. LẤY GIỎ HÀNG: Tự động cập nhật số lượng nếu kho thực tế thay đổi (Do người khác mua)
        public async Task<ShoppingCart> GetCartAsync(string userId)
        {
            var cart = await _context.ShoppingCarts
                .Include(c => c.CartDetails).ThenInclude(cd => cd.Product)
                    .ThenInclude(p => p.ProductSizes).ThenInclude(ps => ps.Size)
                .FirstOrDefaultAsync(c => c.UserId == userId && !c.IsDeleted);

            if (cart == null)
            {
                return new ShoppingCart { UserId = userId, CartDetails = new List<CartDetail>() };
            }

            bool isModified = false;

            foreach (var item in cart.CartDetails)
            {
                // Tìm kho thực tế dựa trên tên Size (Dùng Trim và ToLower để tránh lệch data)
                var stockRecord = item.Product.ProductSizes
                    .FirstOrDefault(ps => ps.Size.SizeName.Trim().ToLower() == item.Size.Trim().ToLower());

                int actualStock = stockRecord?.Quantity ?? 0;

                // Nếu số lượng trong giỏ lớn hơn kho hiện có (Do thằng khác vừa mua mất)
                if (item.Quantity > actualStock)
                {
                    item.Quantity = actualStock; // Ép về bằng kho thực tế
                    isModified = true;
                }
            }

            if (isModified)
            {
                // Cập nhật lại Database ngay để giỏ hàng của khách B luôn chính xác
                await _context.SaveChangesAsync();
            }

            return cart;
        }

        // 2. THÊM VÀO GIỎ: Fix lỗi so sánh chuỗi và chặn vượt kho
        public async Task<(bool Success, string Message, int CartCount)> AddToCartAsync(string userId, AddToCartDto dto)
        {
            var product = await _context.Products.FindAsync(dto.ProductId);
            if (product == null) return (false, "Sản phẩm không tồn tại.", 0);

            // Check kho thực tế - Dùng Trim/ToLower để fix lỗi "còn hàng mà không thêm được"
            var productSize = await _context.ProductSizes
                .Include(ps => ps.Size)
                .FirstOrDefaultAsync(ps => ps.ProductId == dto.ProductId
                                        && ps.Size != null
                                        && ps.Size.SizeName.Trim().ToLower() == dto.Size.Trim().ToLower());

            if (productSize == null) return (false, $"Sản phẩm không có size {dto.Size}.", 0);
            if (productSize.Quantity <= 0) return (false, "Sản phẩm kích cỡ này đã hết hàng.", 0);

            var cart = await _context.ShoppingCarts
                .Include(c => c.CartDetails)
                .FirstOrDefaultAsync(c => c.UserId == userId && !c.IsDeleted);

            if (cart == null)
            {
                cart = new ShoppingCart { UserId = userId };
                _context.ShoppingCarts.Add(cart);
                await _context.SaveChangesAsync();
            }

            var existingDetail = cart.CartDetails?.FirstOrDefault(cd => cd.ProductId == dto.ProductId
                                                                    && cd.Size.Trim().ToLower() == dto.Size.Trim().ToLower());

            int currentInCart = existingDetail?.Quantity ?? 0;
            int requestedTotal = currentInCart + dto.Quantity;

            // Nếu tổng định thêm vào lớn hơn kho đang có
            if (requestedTotal > productSize.Quantity)
            {
                return (false, $"Kho chỉ còn {productSize.Quantity} sản phẩm. Bạn đã có {currentInCart} trong giỏ.", 0);
            }

            if (existingDetail != null)
            {
                existingDetail.Quantity += dto.Quantity;
            }
            else
            {
                _context.CartDetails.Add(new CartDetail
                {
                    ShoppingCartId = cart.Id,
                    ProductId = dto.ProductId,
                    Quantity = dto.Quantity,
                    UnitPrice = product.Price,
                    Size = dto.Size // Lưu đúng tên size khách chọn
                });
            }

            await _context.SaveChangesAsync();

            // Tính lại tổng số món trong giỏ để trả về Header cập nhật icon giỏ hàng
            int cartCount = await _context.CartDetails
                .Where(cd => cd.ShoppingCart.UserId == userId && !cd.ShoppingCart.IsDeleted)
                .SumAsync(cd => cd.Quantity);

            return (true, "Thêm thành công!", cartCount);
        }

        // 3. CẬP NHẬT SỐ LƯỢNG TRONG GIỎ
        public async Task<(bool Success, string Message, object Data)> UpdateQuantityAsync(string userId, UpdateCartQtyDto dto)
        {
            var cartDetail = await _context.CartDetails
                .Include(cd => cd.ShoppingCart)
                .Include(cd => cd.Product).ThenInclude(p => p.ProductSizes).ThenInclude(ps => ps.Size)
                .FirstOrDefaultAsync(cd => cd.Id == dto.CartDetailId && cd.ShoppingCart.UserId == userId);

            if (cartDetail == null) return (false, "Không tìm thấy sản phẩm trong giỏ.", null!);

            // Lấy kho thực tế
            var productSize = cartDetail.Product.ProductSizes
                .FirstOrDefault(ps => ps.Size.SizeName.Trim().ToLower() == cartDetail.Size.Trim().ToLower());

            int maxQty = productSize?.Quantity ?? 0;

            if (dto.Quantity > maxQty)
            {
                cartDetail.Quantity = maxQty; // Tự động đưa về max nếu khách cố tình nhập tay số lớn
                await _context.SaveChangesAsync();
                return (false, $"Kho chỉ còn tối đa {maxQty} sản phẩm.", new { maxQty });
            }

            if (dto.Quantity <= 0) _context.CartDetails.Remove(cartDetail);
            else cartDetail.Quantity = dto.Quantity;

            await _context.SaveChangesAsync();

            var cart = await GetCartAsync(userId);
            return (true, "", new
            {
                subtotal = cartDetail.UnitPrice * cartDetail.Quantity,
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