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

        public async Task<bool> SaveContactAsync(ContactsFormDto dto)
        {
            try
            {
                // BO: Chuyển đổi từ DTO sang Entity
                var contact = new Contacts
                {
                    FullName = dto.FullName,
                    Email = dto.Email,
                    Phone = dto.Phone,
                    Message = dto.Message,
                    CreatedDate = DateTime.Now,
                    IsRead = false
                };

                // Gọi DAO để lưu xuống Database
                await _homeDAO.AddContactAsync(contact);

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