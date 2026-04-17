using Microsoft.EntityFrameworkCore;
using ShopQuanAo.Data;
using ShopQuanAo.Models.BEAN.Entity;

namespace ShopQuanAo.DAO
{
    public class ContactAdminDAO
    {
        private readonly ApplicationDbContext _context;

        public ContactAdminDAO(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Contacts>> GetAllContactsAsync()
        {
            return await _context.Contacts.OrderByDescending(c => c.CreatedDate).ToListAsync();
        }

        public async Task<Contacts?> GetContactByIdAsync(int id)
        {
            return await _context.Contacts.FindAsync(id);
        }

        public async Task UpdateContactAsync(Contacts contact)
        {
            _context.Contacts.Update(contact);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteContactAsync(Contacts contact)
        {
            _context.Contacts.Remove(contact);
            await _context.SaveChangesAsync();
        }
    }
}