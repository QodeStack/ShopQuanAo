using ShopQuanAo.Models.BEAN.DTO;
using ShopQuanAo.Models.BEAN.Entity;
using ShopQuanAo.DAO; // Gọi tầng DAO vào

namespace ShopQuanAo.BO
{
    public class ContactAdminService
    {
        private readonly ContactAdminDAO _contactDAO;

        public ContactAdminService(ContactAdminDAO contactDAO)
        {
            _contactDAO = contactDAO;
        }

        public async Task<List<Contacts>> GetAllContactsAsync()
        {
            return await _contactDAO.GetAllContactsAsync();
        }

        // SỬA TẠI ĐÂY: Tham số truyền vào phải là ContactsReplyDto (có chữ s)
        public async Task<dynamic> ReplyToContactAsync(ContactsReplyDto dto)
        {
            var contact = await _contactDAO.GetContactByIdAsync(dto.Id);
            if (contact == null) return new { Success = false, Message = "Không tìm thấy!" };

            try
            {
                contact.AdminReply = dto.AdminReply;
                contact.IsRead = true;

                await _contactDAO.UpdateContactAsync(contact);
                return new { Success = true, Message = "Thành công" };
            }
            catch (Exception ex)
            {
                return new { Success = false, Message = ex.Message };
            }
        }

        public async Task<bool> DeleteContactAsync(int id)
        {
            var contact = await _contactDAO.GetContactByIdAsync(id);
            if (contact == null) return false;

            await _contactDAO.DeleteContactAsync(contact);
            return true;
        }
    }
}