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