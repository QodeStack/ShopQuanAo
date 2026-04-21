using Microsoft.EntityFrameworkCore;
using ShopQuanAo.Data;
using ShopQuanAo.Models.BEAN.Entity;

namespace ShopQuanAo.DAO
{
    public class CustomerDAO
    {
        private readonly ApplicationDbContext _context;

        public CustomerDAO(ApplicationDbContext context)
        {
            _context = context;
        }

        // Lấy tổng số lượng đơn hàng để BO tính toán số trang
        public async Task<int> GetOrderCountAsync(string userId, string status)
        {
            var query = _context.Orders.Where(o => o.UserId == userId && !o.IsDeleted);

            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(o => o.OrderStatus.StatusName == status);

            return await query.CountAsync();
        }

        // Lấy dữ liệu đơn hàng đã được phân trang
        public async Task<List<Order>> GetPagedOrdersAsync(string userId, string status, int skip, int take)
        {
            var query = _context.Orders
                .Include(o => o.OrderStatus)
                .Include(o => o.OrderDetails).ThenInclude(od => od.Product)
                .Where(o => o.UserId == userId && !o.IsDeleted);

            if (!string.IsNullOrWhiteSpace(status))
                query = query.Where(o => o.OrderStatus.StatusName == status);

            return await query.OrderByDescending(o => o.CreateTime)
                              .Skip(skip)
                              .Take(take)
                              .ToListAsync();
        }

        public async Task<Order?> GetOrderWithDetailsAsync(int orderId, string userId)
        {
            return await _context.Orders
                .Include(o => o.OrderStatus)
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);
        }

        public async Task<Order?> GetOrderByIdAsync(int orderId, string userId)
        {
            return await _context.Orders.FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);
        }

        public async Task<OrderStatus?> GetOrderStatusByNameAsync(string statusName)
        {
            return await _context.OrderStatuses.FirstOrDefaultAsync(s => s.StatusName == statusName);
        }

        public async Task<ProductSize?> GetProductSizeAsync(int productId, string sizeName)
        {
            return await _context.ProductSizes
                .FirstOrDefaultAsync(ps => ps.ProductId == productId && ps.Size.SizeName == sizeName);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
        public async Task<Voucher?> GetVoucherByCodeAsync(string code)
        {
            return await _context.Vouchers.FirstOrDefaultAsync(v => v.Code == code);
        }
        public async Task<List<Order>> GetExpiredPendingOrdersAsync(string userId)
        {
            // Lấy mốc thời gian cách đây 10 phút
            var tenMinsAgo = DateTime.Now.AddMinutes(-10);

            return await _context.Orders
                .Where(o => o.UserId == userId
                         && o.OrderStatus.StatusName == "Chờ xác nhận"
                         && !o.IsPaid
                         && o.PaymentMethod != "COD"
                         && o.CreateTime <= tenMinsAgo)
                .ToListAsync();
        }
    }
}