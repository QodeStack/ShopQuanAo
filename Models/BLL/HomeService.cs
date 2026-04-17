using ShopQuanAo.Data;
using ShopQuanAo.Models.Entity;
using ShopQuanAo.Models.DTO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Linq;// Cần thêm dòng này để dùng được ToListAsync()

namespace ShopQuanAo.Services
{
	public class HomeService
	{
		private readonly ApplicationDbContext _context;

		public HomeService(ApplicationDbContext context)
		{
			_context = context;
		}

		// --- HÀM MỚI: Lấy danh sách sản phẩm đang Sale ---
		public async Task<List<Product>> GetSaleProductsAsync()
		{
			// Chú ý: Cả tên List<Product> và _context.Product (hoặc Products) đều phải khớp
			return await _context.Products
			.OrderByDescending(p => p.Id) // Cứ lấy 10 cái mới nhất hiện ra
			.Take(10)
			.ToListAsync();
		}

		// Hàm lưu liên hệ (Giữ nguyên của Quốc, đã khớp ContactsFormDto)
		public async Task<bool> SaveContactAsync(ContactsFormDto dto)
		{
			try
			{
				var contact = new Contacts
				{
					FullName = dto.FullName,
					Email = dto.Email,
					Phone = dto.Phone,
					Message = dto.Message,
					CreatedDate = DateTime.Now,
					IsRead = false
				};

				_context.Contacts.Add(contact);
				await _context.SaveChangesAsync();
				return true;
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine(ex.Message);
				return false;
			}
		}
	}
}