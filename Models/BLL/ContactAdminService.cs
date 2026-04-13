using ShopQuanAo.Data;
using ShopQuanAo.Models.DTO; // Đảm bảo có dòng này
using Microsoft.EntityFrameworkCore;

namespace ShopQuanAo.Services
{
	public class ContactAdminService
	{
		private readonly ApplicationDbContext _context;

		public ContactAdminService(ApplicationDbContext context)
		{
			_context = context;
		}

		public async Task<List<ShopQuanAo.Models.Entity.Contacts>> GetAllContactsAsync()
		{
			return await _context.Contacts.OrderByDescending(c => c.CreatedDate).ToListAsync();
		}

		// SỬA TẠI ĐÂY: Tham số truyền vào phải là ContactsReplyDto (có chữ s)
		public async Task<dynamic> ReplyToContactAsync(ContactsReplyDto dto)
		{
			var contact = await _context.Contacts.FindAsync(dto.Id);
			if (contact == null) return new { Success = false, Message = "Không tìm thấy!" };

			try
			{
				contact.AdminReply = dto.AdminReply;
				contact.IsRead = true;

				_context.Contacts.Update(contact);
				await _context.SaveChangesAsync();
				return new { Success = true, Message = "Thành công" };
			}
			catch (Exception ex)
			{
				return new { Success = false, Message = ex.Message };
			}
		}

		public async Task<bool> DeleteContactAsync(int id)
		{
			var contact = await _context.Contacts.FindAsync(id);
			if (contact == null) return false;

			_context.Contacts.Remove(contact);
			await _context.SaveChangesAsync();
			return true;
		}
	}
}