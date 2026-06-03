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

        public async Task<List<Categories>> GetAllCategoriesAsync()
        {
            return await _context.Categories.ToListAsync();
        }

        public async Task<int> CountDistinctProductsAsync()
        {
            return await _context.Products.Select(p => p.ProductName).Distinct().CountAsync();
        }
        #endregion

        #region Category Management
        public async Task<Categories?> GetCategoryByIdAsync(int id)
        {
            return await _context.Categories.FindAsync(id);
        }

        public async Task AddCategoryAsync(Categories category)
        {
            _context.Categories.Add(category);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateCategoryAsync(Categories category)
        {
            _context.Categories.Update(category);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> HasProductsInCategoryAsync(int categoryId)
        {
            return await _context.Products.AnyAsync(p => p.CategoryId == categoryId);
        }

        public async Task DeleteCategoryAsync(Categories category)
        {
            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> IsCategoryNameExistAsync(string categoryName)
        {
            return await _context.Categories.AnyAsync(c => c.CategoryName.ToLower() == categoryName.ToLower());
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

        // Phiên bản KHÔNG SaveChanges — dùng trong UpdateProductAsync để tránh EF tracking conflict
        public Task RemoveProductSizesWithoutSaveAsync(IEnumerable<ProductSize> productSizes)
        {
            _context.ProductSizes.RemoveRange(productSizes);
            return Task.CompletedTask;
        }

        public async Task AddSizeWithoutSaveAsync(Size size)
        {
            _context.Sizes.Add(size);
            // Cần SaveChanges ngay để có size.Id khi tạo ProductSize
            await _context.SaveChangesAsync();
        }

        public void AddProductSizeWithoutSave(ProductSize productSize)
        {
            _context.ProductSizes.Add(productSize);
            // KHÔNG SaveChanges — để gom vào 1 lần duy nhất ở cuối UpdateProductAsync
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
        public async Task<List<Product>> GetAllProduct()
        {
            return await _context.Products.ToListAsync();
        }
        public async Task<List<Product>> GetAllProductInSale()
        {
            return await _context.SaleCampaigns
        .SelectMany(c => c.Products)
        .Distinct()
        .ToListAsync();
        }
        #endregion

        #region Sale & Campaign Management
        public async Task<List<SaleCampaign>> GetAllCampaignsAsync()
        {
            return await _context.SaleCampaigns.Include(c => c.Products).ToListAsync();
        }

        public async Task CreateCampaignAsync(SaleCampaign campaign, List<int> productIds, List<int> salePrices)
        {
            _context.SaleCampaigns.Add(campaign);
            await _context.SaveChangesAsync();
            for (int i = 0; i < productIds.Count; i++)
            {
                var product = await _context.Products.FindAsync(productIds[i]);
                if (product != null)
                {
                    product.SaleCampaignId = campaign.Id;
                    product.SalePrice = salePrices[i];
                    _context.Products.Update(product);
                }
            }
            await _context.SaveChangesAsync();
        }
        public async Task<bool> IsCampaignNameExistAsync(string name)
        {
            return await _context.SaleCampaigns.AnyAsync(c => c.CampaignName.ToLower() == name.ToLower());
        }
        public async Task<List<string>> GetProductActiveCampaignWarningsAsync(List<int> productIds)
        {
            var productsInCampaigns = await _context.Products
                .Where(p => productIds.Contains(p.Id) && p.SaleCampaignId != null)
                .Select(p => new {
                    p.ProductName,
                    CampaignName = _context.SaleCampaigns.FirstOrDefault(c => c.Id == p.SaleCampaignId).CampaignName
                }).ToListAsync();

            return productsInCampaigns
                .Select(x => $"Sản phẩm '{x.ProductName}' đang nằm trong chiến dịch '{x.CampaignName ?? "Không rõ"}'")
                .ToList();
        }
        public async Task<SaleCampaign?> GetCampaignByIdAsync(int id)
        {
            return await _context.SaleCampaigns
                .Include(c => c.Products)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task UpdateCampaignAsync(SaleCampaign campaign)
        {
            _context.SaleCampaigns.Update(campaign);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteCampaignAsync(SaleCampaign campaign)
        {
            if (campaign.Products != null && campaign.Products.Any())
            {
                foreach (var product in campaign.Products)
                {
                    product.SaleCampaignId = null;
                    product.SalePrice = 0;
                    _context.Products.Update(product);
                }
            }
            _context.SaleCampaigns.Remove(campaign);
            await _context.SaveChangesAsync();
        }
        public async Task<List<Product>> GetProductsForCampaignEditAsync(int campaignId)
        {
            return await _context.Products
                .Where(p => p.SaleCampaignId == null || p.SaleCampaignId == campaignId)
                .ToListAsync();
        }
        #endregion

        #region Voucher Management
        public async Task<List<Voucher>> GetAllVouchersAsync()
        {
            return await _context.Vouchers.OrderByDescending(v => v.Id).ToListAsync();
        }

        public async Task<List<Voucher>> GetActiveVouchersAsync()
        {
            return await _context.Vouchers.Where(p => p.IsActive && p.Quantity > 0 && p.IsPublic).ToListAsync();
        }

        public async Task<Voucher?> GetVoucherByIdAsync(int id)
        {
            return await _context.Vouchers.FindAsync(id);
        }

        public async Task AddVoucherAsync(Voucher voucher)
        {
            _context.Vouchers.Add(voucher);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateVoucherAsync(Voucher voucher)
        {
            _context.Vouchers.Update(voucher);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteVoucherAsync(Voucher voucher)
        {
            _context.Vouchers.Remove(voucher);
            await _context.SaveChangesAsync();
        }

        // HÀM MỚI: Check trùng mã Voucher
        public async Task<bool> IsVoucherCodeExistAsync(string code)
        {
            return await _context.Vouchers.AnyAsync(v => v.Code.ToLower() == code.ToLower());
        }
        #endregion

        #region Order Management
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
        #endregion

        #region Contact Management
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

        #region User & Cart Management
        public async Task RemoveCartsByUserIdAsync(string userId)
        {
            var carts = _context.ShoppingCarts.Where(c => c.UserId == userId);
            _context.ShoppingCarts.RemoveRange(carts);
            await _context.SaveChangesAsync();
        }
        #endregion

        #region General Operations
        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
        #endregion
    }
}