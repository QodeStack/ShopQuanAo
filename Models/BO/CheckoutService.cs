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

        public async Task<(bool Success, string Message, int OrderId)> PlaceOrderAsync(string userId, PlaceOrderDto dto)
        {
            // 1. Kiểm tra đầu vào cơ bản
            if (dto == null) return (false, "Dữ liệu không hợp lệ.", 0);

            // Lấy giỏ hàng dựa trên các ID sản phẩm đã chọn
            var cart = await _checkoutDAO.GetCartForCheckoutAsync(userId, dto.SelectedIds);

            if (cart == null || cart.CartDetails == null || !cart.CartDetails.Any())
                return (false, "Giỏ hàng trống hoặc sản phẩm không tồn tại.", 0);

            // 2. Tính toán tiền nong
            double originalTotal = cart.CartDetails.Sum(cd => cd.UnitPrice * cd.Quantity);
            double discountAmount = 0;
            string appliedVoucherCode = null;

            // Logic xử lý Voucher (Chống hack)
            if (!string.IsNullOrWhiteSpace(dto.VoucherCode))
            {
                var voucher = await _checkoutDAO.GetVoucherByCodeAsync(dto.VoucherCode);

                if (voucher != null && voucher.IsActive && voucher.Quantity > 0 && originalTotal >= voucher.MinOrderAmount)
                {
                    discountAmount = voucher.DiscountAmount;
                    appliedVoucherCode = voucher.Code;

                    // Cập nhật số lượng Voucher
                    voucher.Quantity -= 1;
                    _checkoutDAO.UpdateVoucher(voucher);
                }
            }

            // 3. Khởi tạo đối tượng đơn hàng
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
                OrderStatusId = 1, // Mặc định: Chờ xác nhận

                // Lưu thông tin tài chính
                VoucherCode = appliedVoucherCode,
                DiscountAmount = discountAmount,
                TotalAmount = originalTotal - discountAmount // QUAN TRỌNG: Lưu tổng tiền thực tế
            };

            // Lưu Order trước để có OrderId
            _checkoutDAO.AddOrder(order);
            await _checkoutDAO.SaveChangesAsync();

            // 4. Xử lý chi tiết đơn hàng và Kho
            foreach (var item in cart.CartDetails)
            {
                // Kiểm tra tồn kho trước khi trừ (Nghiệp vụ quan trọng của BO)
                var productSize = await _checkoutDAO.GetProductSizeAsync(item.ProductId, item.Size);
                if (productSize == null || productSize.Quantity < item.Quantity)
                {
                    // Nếu muốn chặt chẽ, bạn có thể return lỗi ở đây để khách chọn lại SL
                    // return (false, $"Sản phẩm {item.Product.ProductName} không đủ số lượng trong kho.", 0);
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

                // Trừ kho
                if (productSize != null)
                {
                    productSize.Quantity -= item.Quantity;
                    if (productSize.Quantity < 0) productSize.Quantity = 0;
                    // Lưu ý: Đảm bảo DAO của bạn theo dõi được sự thay đổi của productSize để SaveChanges có tác dụng
                }
            }

            // 5. Dọn dẹp giỏ hàng (Chỉ xóa những items vừa đặt)
            _checkoutDAO.RemoveCartDetails(cart.CartDetails);

            // Chốt tất cả thay đổi vào DB
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