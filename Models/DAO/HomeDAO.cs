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

        public async Task AddContactAsync(Contacts contact)
        {
            _context.Contacts.Add(contact);
            await _context.SaveChangesAsync();
        }
    }
}