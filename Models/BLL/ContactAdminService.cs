using Microsoft.EntityFrameworkCore;
using ShopQuanAo.Data;
using ShopQuanAo.Models.DTO;
using ShopQuanAo.Models.Entity;

namespace ShopQuanAo.Services
{
    public class ContactAdminService
    {
        private readonly ApplicationDbContext _context;

        public ContactAdminService(ApplicationDbContext context)
        {
            _context = context;
        }

        // Lấy danh sách liên hệ sắp xếp theo ngày mới nhất
        public async Task<List<Contacts>> GetAllContactsAsync()
        {
            return await _context.Contacts
                .OrderByDescending(c => c.CreatedDate)
                .ToListAsync();
        }

        // Xử lý logic phản hồi
        public async Task<(bool Success, string Message)> ReplyToContactAsync(ContactReplyDto dto)
        {
            var contact = await _context.Contacts.FindAsync(dto.Id);
            if (contact == null) return (false, "Không tìm thấy tin nhắn.");

            if (string.IsNullOrWhiteSpace(dto.AdminReply))
                return (false, "Nội dung phản hồi không được để trống.");

            contact.AdminReply = dto.AdminReply;
            contact.IsRead = true;

            _context.Update(contact);
            await _context.SaveChangesAsync();
            return (true, "Phản hồi thành công.");
        }

        // Xử lý xóa tin nhắn
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