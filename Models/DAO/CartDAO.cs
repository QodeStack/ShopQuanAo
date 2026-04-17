using Microsoft.EntityFrameworkCore;
using ShopQuanAo.Data;
using ShopQuanAo.Models.BEAN.Entity;

namespace ShopQuanAo.DAO
{
    public class CartDAO
    {
        private readonly ApplicationDbContext _context;

        public CartDAO(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ShoppingCart?> GetCartWithDetailsAsync(string userId)
        {
            return await _context.ShoppingCarts
                .Include(c => c.CartDetails).ThenInclude(cd => cd.Product)
                    .ThenInclude(p => p.ProductSizes).ThenInclude(ps => ps.Size)
                .FirstOrDefaultAsync(c => c.UserId == userId && !c.IsDeleted);
        }

        public async Task<Product?> GetProductAsync(int productId)
        {
            return await _context.Products.FindAsync(productId);
        }

        public async Task<ProductSize?> GetProductSizeAsync(int productId, string sizeName)
        {
            return await _context.ProductSizes
                .Include(ps => ps.Size)
                .FirstOrDefaultAsync(ps => ps.ProductId == productId
                                        && ps.Size != null
                                        && ps.Size.SizeName.Trim().ToLower() == sizeName.Trim().ToLower());
        }

        public async Task<ShoppingCart?> GetCartAsync(string userId)
        {
            return await _context.ShoppingCarts
                .Include(c => c.CartDetails)
                .FirstOrDefaultAsync(c => c.UserId == userId && !c.IsDeleted);
        }

        public void AddCart(ShoppingCart cart)
        {
            _context.ShoppingCarts.Add(cart);
        }

        public void AddCartDetail(CartDetail detail)
        {
            _context.CartDetails.Add(detail);
        }

        public async Task<CartDetail?> GetCartDetailAsync(int cartDetailId, string userId)
        {
            return await _context.CartDetails
                .Include(cd => cd.ShoppingCart)
                .Include(cd => cd.Product).ThenInclude(p => p.ProductSizes).ThenInclude(ps => ps.Size)
                .FirstOrDefaultAsync(cd => cd.Id == cartDetailId && cd.ShoppingCart.UserId == userId);
        }

        public void RemoveCartDetail(CartDetail cartDetail)
        {
            _context.CartDetails.Remove(cartDetail);
        }

        public void RemoveCartDetails(IEnumerable<CartDetail> cartDetails)
        {
            _context.CartDetails.RemoveRange(cartDetails);
        }

        public async Task<int> GetTotalCartCountAsync(string userId)
        {
            return await _context.CartDetails
                .Where(cd => cd.ShoppingCart.UserId == userId && !cd.ShoppingCart.IsDeleted)
                .SumAsync(cd => cd.Quantity);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}