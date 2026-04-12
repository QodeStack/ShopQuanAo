using Microsoft.EntityFrameworkCore;
using ShopQuanAo.Data;
using ShopQuanAo.Models.Entity;
using ShopQuanAo.Models.DTO;

namespace ShopQuanAo.Services
{
    public class CheckoutService
    {
        private readonly ApplicationDbContext _context;

        public CheckoutService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ShoppingCart?> GetCartForCheckoutAsync(string userId)
        {
            return await _context.ShoppingCarts
                .Include(c => c.CartDetails).ThenInclude(cd => cd.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId && !c.IsDeleted);
        }

        public async Task<(bool Success, string Message, int OrderId)> PlaceOrderAsync(string userId, PlaceOrderDto dto)
        {
            var cart = await GetCartForCheckoutAsync(userId);
            if (cart == null || cart.CartDetails == null || !cart.CartDetails.Any())
                return (false, "Giỏ hàng trống.", 0);

            // 1. Tạo đơn hàng mới
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

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

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
                _context.OrderDetails.Add(orderDetail);

                var productSize = await _context.ProductSizes
                    .FirstOrDefaultAsync(ps => ps.ProductId == item.ProductId && ps.Size.SizeName == item.Size);

                if (productSize != null)
                {
                    productSize.Quantity -= item.Quantity;
                    if (productSize.Quantity < 0) productSize.Quantity = 0;
                }
            }

            // 3. Xóa các mặt hàng trong giỏ sau khi đặt hàng thành công
            _context.CartDetails.RemoveRange(cart.CartDetails);
            await _context.SaveChangesAsync();

            return (true, "Đặt hàng thành công", order.Id);
        }
    }
}