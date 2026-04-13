using ShopQuanAo.Data;
using ShopQuanAo.Models.Entity;
using ShopQuanAo.Models.DTO;

namespace ShopQuanAo.Services
{
	public class HomeService
	{
		private readonly ApplicationDbContext _context;

		public HomeService(ApplicationDbContext context)
		{
			_context = context;
		}

		// Đổi chỗ này thành ContactsFormDto cho khớp với file bạn vừa gửi
		public async Task<bool> SaveContactAsync(ContactsFormDto dto)
		{
			try
			{
				var contact = new Contacts
				{
					FullName = dto.FullName,
					Email = dto.Email,
					Phone = dto.Phone, // Sẽ HẾT LỖI vì đã khớp với DTO có chữ s
					Message = dto.Message,
					CreatedDate = DateTime.Now,
					IsRead = false
				};

				_context.Contacts.Add(contact);
				await _context.SaveChangesAsync(); // Chốt lưu vào SQL
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