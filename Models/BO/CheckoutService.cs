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

        // ĐÃ SỬA: Thêm tham số selectedIds và gọi đúng hàm filter của DAO
        public async Task<ShoppingCart?> GetCartForCheckoutAsync(string userId, List<int> selectedIds)
        {
            if (selectedIds == null || !selectedIds.Any())
            {
                return await _checkoutDAO.GetCartWithDetailsAsync(userId); // Nếu ko có ID nào, lấy tất
            }
            return await _checkoutDAO.GetCartForCheckoutAsync(userId, selectedIds);
        }

        // ĐÃ SỬA: Bỏ tham số thừa, chỉ dùng dto.SelectedIds
        public async Task<(bool Success, string Message, int OrderId)> PlaceOrderAsync(string userId, PlaceOrderDto dto)
        {
            var cart = await _checkoutDAO.GetCartForCheckoutAsync(userId, dto.SelectedIds);

            if (cart == null || cart.CartDetails == null || !cart.CartDetails.Any())
                return (false, "Giỏ hàng trống.", 0);

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
                OrderStatusId = 1
            };

            _checkoutDAO.AddOrder(order);
            await _checkoutDAO.SaveChangesAsync();

            foreach (var item in cart.CartDetails)
            {
                var orderDetail = new OrderDetail
                {
                    OrderId = order.Id,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    Size = item.Size
                };

                _checkoutDAO.AddOrderDetail(orderDetail);

                var productSize = await _checkoutDAO.GetProductSizeAsync(item.ProductId, item.Size);
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
        public async Task<Order?> GetLatestOrderAsync(string userId)
        {
            return await _checkoutDAO.GetLatestOrderAsync(userId);
        }
    }
}