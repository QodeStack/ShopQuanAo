using ShopQuanAo.Models.BEAN.DTO;
using ShopQuanAo.Models.BEAN.Entity;
using ShopQuanAo.DAO;

namespace ShopQuanAo.BO
{
    public class CustomerService
    {
        private readonly CustomerDAO _customerDAO;
        private const int PageSize = 5;

        public CustomerService(CustomerDAO customerDAO)
        {
            _customerDAO = customerDAO;
        }

        public async Task<CustomerOrderPagedDto> GetOrdersAsync(string userId, string status, int page)
        {
            // 1. Nhờ DAO đếm tổng số đơn để tính toán
            var total = await _customerDAO.GetOrderCountAsync(userId, status);

            // 2. Não bộ (BO) tính toán phân trang
            var totalPages = (int)Math.Ceiling(total / (double)PageSize);
            page = Math.Clamp(page, 1, Math.Max(1, totalPages));
            int skip = (page - 1) * PageSize;

            // 3. Yêu cầu DAO lấy chính xác khúc dữ liệu cần thiết
            var orders = await _customerDAO.GetPagedOrdersAsync(userId, status, skip, PageSize);

            return new CustomerOrderPagedDto
            {
                Orders = orders,
                CurrentPage = page,
                TotalPages = totalPages,
                CurrentStatus = status
            };
        }

        public async Task<(bool Success, string Message)> CancelOrderAsync(string userId, int orderId)
        {
            var order = await _customerDAO.GetOrderWithDetailsAsync(orderId, userId);

            if (order == null) return (false, "Không tìm thấy đơn hàng.");

            // Áp dụng luật kinh doanh: Chỉ cho hủy nếu đang chờ xác nhận
            if (order.OrderStatus?.StatusName == "Chờ xác nhận")
            {
                var cancelStatus = await _customerDAO.GetOrderStatusByNameAsync("Đã hủy");
                if (cancelStatus == null) return (false, "Hệ thống thiếu trạng thái 'Đã hủy'.");

                order.OrderStatusId = cancelStatus.Id;

                // Hoàn lại số lượng vào kho khi hủy
                foreach (var detail in order.OrderDetails)
                {
                    var productSize = await _customerDAO.GetProductSizeAsync(detail.ProductId, detail.Size);
                    if (productSize != null)
                    {
                        productSize.Quantity += detail.Quantity;
                    }
                }

                await _customerDAO.SaveChangesAsync();
                return (true, "Đã hủy đơn hàng thành công.");
            }

            return (false, "Không thể hủy đơn hàng ở trạng thái hiện tại.");
        }

        public async Task<(bool Success, string Message)> ConfirmReceivedAsync(string userId, int orderId)
        {
            var order = await _customerDAO.GetOrderByIdAsync(orderId, userId);
            if (order == null) return (false, "Không tìm thấy đơn hàng.");

            var completedStatus = await _customerDAO.GetOrderStatusByNameAsync("Đã hoàn thành");
            if (completedStatus == null) return (false, "Lỗi hệ thống.");

            order.OrderStatusId = completedStatus.Id;
            order.IsPaid = true;

            await _customerDAO.SaveChangesAsync();
            return (true, "Cảm ơn bạn đã xác nhận nhận hàng!");
        }
    }
}