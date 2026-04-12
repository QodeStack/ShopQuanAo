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

        public async Task<bool> SaveContactAsync(ContactFormDto dto)
        {
            try
            {
                var contact = new Contacts // Đảm bảo đúng tên Class Entity của bạn
                {
                    FullName = dto.FullName,
                    Email = dto.Email,
                    Message = dto.Message,
                    CreatedDate = DateTime.Now,
                    IsRead = false
                };

                _context.Contacts.Add(contact);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}