using Microsoft.EntityFrameworkCore;
using ShopQuanAo.Data;
using ShopQuanAo.Models.BEAN.Entity;

namespace ShopQuanAo.DAO
{
    public class CheckoutDAO
    {
        private readonly ApplicationDbContext _context;

        public CheckoutDAO(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ShoppingCart?> GetCartWithDetailsAsync(string userId)
        {
            return await _context.ShoppingCarts
                .Include(c => c.CartDetails).ThenInclude(cd => cd.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId && !c.IsDeleted);
        }

        // HÀM MỚI: Lấy giỏ hàng nhưng CHỈ bao gồm những món được tick chọn
        public async Task<ShoppingCart?> GetCartForCheckoutAsync(string userId, List<int> selectedIds)
        {
            return await _context.ShoppingCarts
                // Bộ lọc thần thánh nằm ở đây: Chỉ kéo lên những CartDetail có Id nằm trong danh sách selectedIds
                .Include(c => c.CartDetails.Where(cd => selectedIds.Contains(cd.Id)))
                .ThenInclude(cd => cd.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId && !c.IsDeleted);
        }
        // Lấy đơn hàng gần nhất của user để fill tự động
        public async Task<Order?> GetLatestOrderAsync(string userId)
        {
            return await _context.Orders
                .Where(o => o.UserId == userId && !o.IsDeleted)
                .OrderByDescending(o => o.CreateTime) // Sắp xếp giảm dần theo thời gian tạo
                .FirstOrDefaultAsync(); // Lấy cái mới nhất trên cùng
        }
        public void AddOrder(Order order)
        {
            _context.Orders.Add(order);
        }

        public void AddOrderDetail(OrderDetail orderDetail)
        {
            _context.OrderDetails.Add(orderDetail);
        }

        public async Task<ProductSize?> GetProductSizeAsync(int productId, string sizeName)
        {
            return await _context.ProductSizes
                .FirstOrDefaultAsync(ps => ps.ProductId == productId && ps.Size.SizeName == sizeName);
        }

        public void RemoveCartDetails(IEnumerable<CartDetail> cartDetails)
        {
            _context.CartDetails.RemoveRange(cartDetails);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}