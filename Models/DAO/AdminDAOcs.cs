using Microsoft.EntityFrameworkCore;
using ShopQuanAo.Data;
using ShopQuanAo.Models.BEAN.Entity;

namespace ShopQuanAo.DAO
{
    public class AdminDAO
    {
        private readonly ApplicationDbContext _context;

        public AdminDAO(ApplicationDbContext context)
        {
            _context = context;
        }

        #region Dashboard & Stats
        public async Task<List<OrderDetail>> GetPaidOrderDetailsAsync(DateTime start, DateTime endOfPeriod)
        {
            return await _context.OrderDetails
                .Include(od => od.Order)
                .Include(od => od.Product)
                .Where(od => od.Order.IsPaid
                        && od.Order.CreateTime >= start
                        && od.Order.CreateTime <= endOfPeriod
                        && !od.Order.IsDeleted)
                .ToListAsync();
        }

        public async Task<int> CountValidOrdersAsync(DateTime start, DateTime endOfPeriod)
        {
            return await _context.Orders
                .CountAsync(o => o.CreateTime >= start && o.CreateTime <= endOfPeriod && !o.IsDeleted);
        }

        // Lưu ý: Dùng đúng tên Categories có 's' theo cấu trúc file của bạn
        public async Task<List<Categories>> GetAllCategoriesAsync()
        {
            return await _context.Categories.ToListAsync();
        }

        public async Task<int> CountDistinctProductsAsync()
        {
            return await _context.Products.Select(p => p.ProductName).Distinct().CountAsync();
        }
        #endregion

        #region User Management (Xóa giỏ hàng)
        public async Task RemoveCartsByUserIdAsync(string userId)
        {
            var carts = _context.ShoppingCarts.Where(c => c.UserId == userId);
            _context.ShoppingCarts.RemoveRange(carts);
            await _context.SaveChangesAsync();
        }
        #endregion

        #region Product & Size Management
        public async Task<List<Product>> GetAllProductsWithRelationsAsync()
        {
            return await _context.Products
                .Include(p => p.Category)
                .Include(p => p.ProductSizes)
                .ThenInclude(ps => ps.Size)
                .ToListAsync();
        }

        public async Task<bool> IsProductExistAsync(string productName)
        {
            return await _context.Products.AnyAsync(p => p.ProductName.ToLower() == productName.ToLower());
        }

        public async Task AddProductAsync(Product product)
        {
            _context.Products.Add(product);
            await _context.SaveChangesAsync();
        }

        public async Task<Size?> GetSizeByNameAsync(string sizeName)
        {
            return await _context.Sizes.FirstOrDefaultAsync(x => x.SizeName == sizeName);
        }

        public async Task AddSizeAsync(Size size)
        {
            _context.Sizes.Add(size);
            await _context.SaveChangesAsync();
        }

        public async Task AddProductSizeAsync(ProductSize productSize)
        {
            _context.ProductSizes.Add(productSize);
            await _context.SaveChangesAsync();
        }

        public async Task<Product?> GetProductByIdAsync(int id)
        {
            return await _context.Products.Include(p => p.ProductSizes).FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task RemoveProductSizesAsync(IEnumerable<ProductSize> productSizes)
        {
            _context.ProductSizes.RemoveRange(productSizes);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> DeleteProductDependenciesAsync(int productId)
        {
            try
            {
                var orderDetails = _context.OrderDetails.Where(od => od.ProductId == productId);
                _context.OrderDetails.RemoveRange(orderDetails);

                var productSizes = _context.ProductSizes.Where(ps => ps.ProductId == productId);
                _context.ProductSizes.RemoveRange(productSizes);

                var cartDetails = _context.CartDetails.Where(cd => cd.ProductId == productId);
                _context.CartDetails.RemoveRange(cartDetails);

                var product = await _context.Products.FindAsync(productId);
                if (product != null) _context.Products.Remove(product);

                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }
        #endregion

        #region Order & Contact Management
        public async Task<List<Order>> GetAllOrdersWithRelationsAsync()
        {
            return await _context.Orders
                .Include(o => o.OrderStatus)
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .Where(o => !o.IsDeleted)
                .OrderByDescending(o => o.CreateTime)
                .ToListAsync();
        }

        public async Task<Order?> GetOrderByIdAsync(int orderId)
        {
            return await _context.Orders.Include(o => o.OrderStatus).FirstOrDefaultAsync(o => o.Id == orderId);
        }

        public async Task<OrderStatus> GetOrderStatusByNameAsync(string statusName)
        {
            return await _context.OrderStatuses.FirstAsync(s => s.StatusName == statusName);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }

        public async Task<bool> DeleteContactAsync(int id)
        {
            try
            {
                var contact = await _context.Contacts.FindAsync(id);
                if (contact == null) return false;

                _context.Contacts.Remove(contact);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }
        #endregion
    }
}