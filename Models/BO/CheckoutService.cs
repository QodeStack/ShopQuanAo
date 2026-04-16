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

        public async Task<ShoppingCart?> GetCartForCheckoutAsync(string userId)
        {
            return await _checkoutDAO.GetCartWithDetailsAsync(userId);
        }

        public async Task<(bool Success, string Message, int OrderId)> PlaceOrderAsync(string userId, PlaceOrderDto dto)
        {
            var cart = await _checkoutDAO.GetCartWithDetailsAsync(userId);

            // BO: Kiểm tra tính hợp lệ của giỏ hàng
            if (cart == null || cart.CartDetails == null || !cart.CartDetails.Any())
                return (false, "Giỏ hàng trống.", 0);

            // 1. Khởi tạo đối tượng đơn hàng mới (BO tạo dữ liệu)
            var order = new Order
            {
                UserId = userId,
                Name = dto.Name,
                Email = dto.Email,
                MobileNumber = dto.MobileNumber,
                Address = dto.Address,
                //Note = dto.Note,
                PaymentMethod = dto.PaymentMethod,
                CreateTime = DateTime.Now,
                IsDeleted = false,
                IsPaid = false,
                OrderStatusId = 1 // Chờ xác nhận
            };

            // Gọi DAO để cất xuống DB và lấy ID đơn hàng
            _checkoutDAO.AddOrder(order);
            await _checkoutDAO.SaveChangesAsync();

            // 2. Chuyển CartDetails sang OrderDetails và trừ tồn kho
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

                // Lấy thông tin kho hiện tại lên để tính toán
                var productSize = await _checkoutDAO.GetProductSizeAsync(item.ProductId, item.Size);

                if (productSize != null)
                {
                    // BO: Nghiệp vụ trừ kho
                    productSize.Quantity -= item.Quantity;
                    if (productSize.Quantity < 0) productSize.Quantity = 0; // Đảm bảo kho không bị âm
                }
            }

            // 3. Xóa các mặt hàng trong giỏ sau khi đặt hàng thành công
            _checkoutDAO.RemoveCartDetails(cart.CartDetails);

            // Chốt giao dịch
            await _checkoutDAO.SaveChangesAsync();

            return (true, "Đặt hàng thành công", order.Id);
        }
    }
}