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
            // Lấy giỏ hàng
            var cart = await _checkoutDAO.GetCartForCheckoutAsync(userId, dto.SelectedIds);

            if (cart == null || cart.CartDetails == null || !cart.CartDetails.Any())
                return (false, "Giỏ hàng trống.", 0);

            // Tính tổng tiền gốc của đơn hàng
            double originalTotal = cart.CartDetails.Sum(cd => cd.UnitPrice * cd.Quantity);
            double discountAmount = 0;
            string appliedVoucherCode = null;

            // ==========================================
            // LOGIC XỬ LÝ VOUCHER (CHỐNG HACK)
            // ==========================================
            if (!string.IsNullOrWhiteSpace(dto.VoucherCode))
            {
                var voucher = await _checkoutDAO.GetVoucherByCodeAsync(dto.VoucherCode);

                // Kiểm tra xem mã có hợp lệ thực sự không
                if (voucher != null && voucher.IsActive && voucher.Quantity > 0 && originalTotal >= voucher.MinOrderAmount)
                {
                    discountAmount = voucher.DiscountAmount;
                    appliedVoucherCode = voucher.Code;

                    // Trừ đi 1 lượt sử dụng của mã này
                    voucher.Quantity -= 1;
                    _checkoutDAO.UpdateVoucher(voucher);
                }
            }
            // ==========================================

            // 1. Khởi tạo đối tượng đơn hàng mới (Đã gộp chung phần Voucher)
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

                // LƯU LẠI VẾT VOUCHER VÀO ĐƠN HÀNG
                VoucherCode = appliedVoucherCode,
                DiscountAmount = discountAmount
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

            // 3. Xóa các mặt hàng đã chọn trong giỏ
            _checkoutDAO.RemoveCartDetails(cart.CartDetails);

            // Chốt giao dịch
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
    }
}