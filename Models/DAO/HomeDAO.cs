using Microsoft.EntityFrameworkCore;
using ShopQuanAo.Data;
using ShopQuanAo.Models.BEAN.Entity;

namespace ShopQuanAo.DAO
{
	public class HomeDAO
	{
		private readonly ApplicationDbContext _context;

		public HomeDAO(ApplicationDbContext context)
		{
			_context = context;
		}

		public async Task<List<Product>> GetSaleProductsAsync()
		{
			return await _context.Products
				.Where(p => p.SalePrice > 0)
				.OrderByDescending(p => p.Id)
				.Take(10)
				.ToListAsync();
		}

		public async Task AddContactAsync(Contacts contact)
		{
			_context.Contacts.Add(contact);
			await _context.SaveChangesAsync();
		}

		public async Task<List<Product>> GetNewArrivalsAsync()
		{
			return await _context.Products
				.OrderByDescending(p => p.Id)
				.Take(8)
				.ToListAsync();
		}

		public async Task<List<Product>> GetBestSellersAsync()
		{
			return await _context.Products
				.OrderBy(p => p.Price)
				.Take(8)
				.ToListAsync();
		}

		// --- THÊM HÀM NÀY ĐỂ HẾT LỖI TẠI SERVICE VÀ CONTROLLER ---
		public async Task<List<Product>> GetProductsByCategoryAsync(string categoryName)
		{
			return await _context.Products
				.Include(p => p.Category)
				// Xóa phần p.Status == true đi
				.Where(p => p.Category.CategoryName == categoryName)
				.OrderByDescending(p => p.Id)
				.Take(10)
				.ToListAsync();
		}
	}
}