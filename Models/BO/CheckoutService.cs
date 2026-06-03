using ShopQuanAo.Models.BEAN.DTO;
using ShopQuanAo.Models.BEAN.Entity;
using ShopQuanAo.DAO;

namespace ShopQuanAo.BO
{
    public class CheckoutService
    {
        private readonly CheckoutDAO _checkoutDAO;

        public CheckoutService(CheckoutDAO checkoutDAO)
        {
            _checkoutDAO = checkoutDAO;
        }
        public async Task<ShoppingCart?> GetCartForCheckoutAsync(string userId, List<int> selectedIds)
        {
            if (selectedIds == null || !selectedIds.Any())
            {
                return await _checkoutDAO.GetCartWithDetailsAsync(userId); 
            }
            return await _checkoutDAO.GetCartForCheckoutAsync(userId, selectedIds);
        }

        public async Task<(bool Success, string Message, int OrderId)> PlaceOrderAsync(string userId, PlaceOrderDto dto)
        {

            if (dto == null) return (false, "Dữ liệu không hợp lệ.", 0);

            var cart = await _checkoutDAO.GetCartForCheckoutAsync(userId, dto.SelectedIds);

            if (cart == null || cart.CartDetails == null || !cart.CartDetails.Any())
                return (false, "Giỏ hàng trống hoặc sản phẩm không tồn tại.", 0);

            double originalTotal = cart.CartDetails.Sum(cd => cd.UnitPrice * cd.Quantity);
            double discountAmount = 0;
            string appliedVoucherCode = null;

            if (!string.IsNullOrWhiteSpace(dto.VoucherCode))
            {
                var voucher = await _checkoutDAO.GetVoucherByCodeAsync(dto.VoucherCode);

                if (voucher != null && voucher.IsActive && voucher.Quantity > 0 && originalTotal >= voucher.MinOrderAmount)
                {
                    discountAmount = Math.Min(originalTotal, voucher.DiscountAmount);
                    appliedVoucherCode = voucher.Code;
                    voucher.Quantity -= 1;
                    _checkoutDAO.UpdateVoucher(voucher);
                }
            }

            var order = new Order
            {
                UserId = userId,
                Name = dto.Name,
                Email = dto.Email,
                MobileNumber = dto.MobileNumber,
                Address = dto.Address,
                PaymentMethod = dto.PaymentMethod,
                CreateTime = DateTime.Now,
                IsDeleted = false,
                IsPaid = false,
                OrderStatusId = 1, 

                VoucherCode = appliedVoucherCode,
                DiscountAmount = discountAmount,
                TotalAmount = originalTotal - discountAmount 
            };

            _checkoutDAO.AddOrder(order);
            await _checkoutDAO.SaveChangesAsync();

            foreach (var item in cart.CartDetails)
            {
                var productSize = await _checkoutDAO.GetProductSizeAsync(item.ProductId, item.Size);
                if (productSize == null || productSize.Quantity < item.Quantity)
                {
                    return (false, "Sản phẩm trong kho không đủ số lượng", 0);
                }

                var orderDetail = new OrderDetail
                {
                    OrderId = order.Id,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    Size = item.Size
                };
                _checkoutDAO.AddOrderDetail(orderDetail);

                if (productSize != null)
                {
                    productSize.Quantity -= item.Quantity;
                    if (productSize.Quantity < 0) productSize.Quantity = 0;
                }
            }

            _checkoutDAO.RemoveCartDetails(cart.CartDetails);

            await _checkoutDAO.SaveChangesAsync();

            return (true, "Đặt hàng thành công", order.Id);
        }
        public async Task<Voucher?> GetVoucherByCodeAsync(string code)
        {
            return await _checkoutDAO.GetVoucherByCodeAsync(code);
        }
        public async Task<Order?> GetLatestOrderAsync(string userId)
        {
            return await _checkoutDAO.GetLatestOrderAsync(userId);
        }
        public async Task<List<Voucher>> GetActiveVouchersAsync()
        {
            return await _checkoutDAO.GetActiveVouchersAsync();
        }
        public async Task<Order?> GetOrderByIdAsync(int orderId)
        {
            return await _checkoutDAO.GetOrderByIdAsync(orderId);
        }
    }
}