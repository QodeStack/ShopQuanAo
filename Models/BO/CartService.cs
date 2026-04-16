using ShopQuanAo.Models.BEAN.DTO;
using ShopQuanAo.Models.BEAN.Entity;
using ShopQuanAo.DAO;

namespace ShopQuanAo.BO
{
    public class CartService
    {
        private readonly CartDAO _cartDAO;

        public CartService(CartDAO cartDAO)
        {
            _cartDAO = cartDAO;
        }

        // 1. LẤY GIỎ HÀNG: Tự động cập nhật số lượng và GIÁ TIỀN (Sale) thực tế
        public async Task<ShoppingCart> GetCartAsync(string userId)
        {
            var cart = await _cartDAO.GetCartWithDetailsAsync(userId);

            if (cart == null)
            {
                return new ShoppingCart { UserId = userId, CartDetails = new List<CartDetail>() };
            }

            bool isModified = false;

            foreach (var item in cart.CartDetails)
            {
                // Tìm kho thực tế dựa trên tên Size
                var stockRecord = item.Product.ProductSizes
                    .FirstOrDefault(ps => ps.Size.SizeName.Trim().ToLower() == item.Size.Trim().ToLower());

                int actualStock = stockRecord?.Quantity ?? 0;

                // Nếu số lượng trong giỏ lớn hơn kho hiện có
                if (item.Quantity > actualStock)
                {
                    item.Quantity = actualStock;
                    isModified = true;
                }

                // FIX SALE: Cập nhật lại giá tiền trong giỏ nếu Admin vừa đổi giá/bật Sale
                double realPrice = item.Product.SalePrice > 0 ? item.Product.SalePrice : item.Product.Price;
                if (item.UnitPrice != realPrice)
                {
                    item.UnitPrice = realPrice;
                    isModified = true;
                }
            }

            if (isModified)
            {
                // Cập nhật lại Database thông qua DAO
                await _cartDAO.SaveChangesAsync();
            }

            return cart;
        }

        // 2. THÊM VÀO GIỎ: Bắt giá Sale ngay từ lúc bốc hàng
        public async Task<(bool Success, string Message, int CartCount)> AddToCartAsync(string userId, AddToCartDto dto)
        {
            var product = await _cartDAO.GetProductAsync(dto.ProductId);
            if (product == null) return (false, "Sản phẩm không tồn tại.", 0);

            var productSize = await _cartDAO.GetProductSizeAsync(dto.ProductId, dto.Size);

            if (productSize == null) return (false, $"Sản phẩm không có size {dto.Size}.", 0);
            if (productSize.Quantity <= 0) return (false, "Sản phẩm kích cỡ này đã hết hàng.", 0);

            var cart = await _cartDAO.GetCartAsync(userId);

            if (cart == null)
            {
                cart = new ShoppingCart { UserId = userId };
                _cartDAO.AddCart(cart);
                await _cartDAO.SaveChangesAsync(); // Lưu để có ID giỏ hàng
            }

            var existingDetail = cart.CartDetails?.FirstOrDefault(cd => cd.ProductId == dto.ProductId
                                                                    && cd.Size.Trim().ToLower() == dto.Size.Trim().ToLower());

            int currentInCart = existingDetail?.Quantity ?? 0;
            int requestedTotal = currentInCart + dto.Quantity;

            if (requestedTotal > productSize.Quantity)
            {
                return (false, $"Kho chỉ còn {productSize.Quantity} sản phẩm. Bạn đã có {currentInCart} trong giỏ.", 0);
            }

            // FIX SALE CHÍNH: Bắt giá Sale để nạp vào giỏ
            double finalPrice = product.SalePrice > 0 ? product.SalePrice : product.Price;

            if (existingDetail != null)
            {
                existingDetail.Quantity += dto.Quantity;
                existingDetail.UnitPrice = finalPrice; // Lỡ khách thêm hàng lúc vừa chạy Sale
            }
            else
            {
                _cartDAO.AddCartDetail(new CartDetail
                {
                    ShoppingCartId = cart.Id,
                    ProductId = dto.ProductId,
                    Quantity = dto.Quantity,
                    UnitPrice = finalPrice, // Lưu đúng giá đã giảm
                    Size = dto.Size
                });
            }

            await _cartDAO.SaveChangesAsync();

            int cartCount = await _cartDAO.GetTotalCartCountAsync(userId);

            return (true, "Thêm thành công!", cartCount);
        }

        // 3. CẬP NHẬT SỐ LƯỢNG TRONG GIỎ
        public async Task<(bool Success, string Message, object Data)> UpdateQuantityAsync(string userId, UpdateCartQtyDto dto)
        {
            // Tách DAO giúp dòng này gọn gàng hơn hẳn
            var cartDetail = await _cartDAO.GetCartDetailAsync(dto.CartDetailId, userId);

            if (cartDetail == null) return (false, "Không tìm thấy sản phẩm trong giỏ.", null!);

            var productSize = cartDetail.Product.ProductSizes
                .FirstOrDefault(ps => ps.Size.SizeName.Trim().ToLower() == cartDetail.Size.Trim().ToLower());

            int maxQty = productSize?.Quantity ?? 0;

            if (dto.Quantity > maxQty)
            {
                cartDetail.Quantity = maxQty;
                await _cartDAO.SaveChangesAsync();
                return (false, $"Kho chỉ còn tối đa {maxQty} sản phẩm.", new { maxQty });
            }

            if (dto.Quantity <= 0)
                _cartDAO.RemoveCartDetail(cartDetail);
            else
                cartDetail.Quantity = dto.Quantity;

            // FIX SALE CHỖ NÀY LUÔN: Đồng bộ lại giá lỡ có thay đổi
            cartDetail.UnitPrice = cartDetail.Product.SalePrice > 0 ? cartDetail.Product.SalePrice : cartDetail.Product.Price;

            await _cartDAO.SaveChangesAsync();

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
            var cartDetail = await _cartDAO.GetCartDetailAsync(cartDetailId, userId);

            if (cartDetail != null)
            {
                _cartDAO.RemoveCartDetail(cartDetail);
                await _cartDAO.SaveChangesAsync();
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
            var cart = await _cartDAO.GetCartAsync(userId);

            if (cart?.CartDetails != null)
            {
                _cartDAO.RemoveCartDetails(cart.CartDetails);
                await _cartDAO.SaveChangesAsync();
            }
        }
    }
}