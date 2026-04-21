using ShopQuanAo.Models.BEAN.DTO;
using ShopQuanAo.Models.BEAN.Entity;
using ShopQuanAo.DAO;

namespace ShopQuanAo.BO
{
	public class HomeService
	{
		private readonly HomeDAO _homeDAO;

		public HomeService(HomeDAO homeDAO)
		{
			_homeDAO = homeDAO;
		}

		public async Task<List<Product>> GetSaleProductsAsync()
		{
			try
			{
				return await _homeDAO.GetSaleProductsAsync();
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine("Lỗi tại HomeService: " + ex.Message);
				return new List<Product>();
			}
		}

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
				await _homeDAO.AddContactAsync(contact);
				return true;
			}
			catch { return false; }
		}

		public async Task<List<Product>> GetNewArrivalsAsync()
		{
			try
			{
				return await _homeDAO.GetNewArrivalsAsync();
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine("Lỗi tại HomeService (NewArrivals): " + ex.Message);
				return new List<Product>();
			}
		}

		public async Task<List<Product>> GetBestSellersAsync()
		{
			try
			{
				return await _homeDAO.GetBestSellersAsync();
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine("Lỗi tại HomeService (BestSellers): " + ex.Message);
				return new List<Product>();
			}
		}

		// --- HÀM MỚI THÊM VÀO ĐÂY ---
		// Hàm này dùng chung cho cả 4 danh mục (Áo thun, Sơ mi, Jean, Quần tây)
		public async Task<List<Product>> GetProductsByCategoryAsync(string categoryName)
		{
			try
			{
				// Gọi sang DAO để lấy dữ liệu
				return await _homeDAO.GetProductsByCategoryAsync(categoryName);
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"Lỗi tại HomeService (Category: {categoryName}): " + ex.Message);
				return new List<Product>();
			}
		}
	}
}