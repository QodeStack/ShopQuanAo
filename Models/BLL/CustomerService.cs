using Microsoft.EntityFrameworkCore;
using ShopQuanAo.Data;
using ShopQuanAo.Models.DTO;
using ShopQuanAo.Models.Entity;

namespace ShopQuanAo.Services
{
    public class CustomerService
    {
        private readonly ApplicationDbContext _context;
        private const int PageSize = 5;

        public CustomerService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<CustomerOrderPagedDto> GetOrdersAsync(string userId, string status, int page)
        {
            var query = _context.Orders
                .Include(o => o.OrderStatus)
                .Include(o => o.OrderDetails).ThenInclude(od => od.Product)
                .Where(o => o.UserId == userId && !o.IsDeleted)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(o => o.OrderStatus.StatusName == status);

            query = query.OrderByDescending(o => o.CreateTime);

            var total = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(total / (double)PageSize);
            page = Math.Clamp(page, 1, Math.Max(1, totalPages));

            var orders = await query
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

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
            var order = await _context.Orders
                .Include(o => o.OrderStatus)
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);

            if (order == null) return (false, "Không tìm thấy đơn hàng.");

            if (order.OrderStatus?.StatusName == "Chờ xác nhận")
            {
                var cancelStatus = await _context.OrderStatuses.FirstOrDefaultAsync(s => s.StatusName == "Đã hủy");
                if (cancelStatus == null) return (false, "Hệ thống thiếu trạng thái 'Đã hủy'.");

                order.OrderStatusId = cancelStatus.Id;

                // Hoàn lại số lượng vào kho khi hủy
                foreach (var detail in order.OrderDetails)
                {
                    var productSize = await _context.ProductSizes
                        .FirstOrDefaultAsync(ps => ps.ProductId == detail.ProductId && ps.Size.SizeName == detail.Size);

                    if (productSize != null) productSize.Quantity += detail.Quantity;
                }

                await _context.SaveChangesAsync();
                return (true, "Đã hủy đơn hàng thành công.");
            }

            return (false, "Không thể hủy đơn hàng ở trạng thái hiện tại.");
        }

        public async Task<(bool Success, string Message)> ConfirmReceivedAsync(string userId, int orderId)
        {
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);
            if (order == null) return (false, "Không tìm thấy đơn hàng.");

            var completedStatus = await _context.OrderStatuses.FirstOrDefaultAsync(s => s.StatusName == "Đã hoàn thành");
            if (completedStatus == null) return (false, "Lỗi hệ thống.");

            order.OrderStatusId = completedStatus.Id;
            order.IsPaid = true;

            await _context.SaveChangesAsync();
            return (true, "Cảm ơn bạn đã xác nhận nhận hàng!");
        }
    }
}