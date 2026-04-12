using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShopQuanAo.Models.DTO;
using ShopQuanAo.Services;

namespace ShopQuanAo.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ContactAdminController : Controller
    {
        private readonly ContactAdminService _service;

        public ContactAdminController(ContactAdminService service)
        {
            _service = service;
        }

        // 1. Trang danh sách phản hồi
        public async Task<IActionResult> Index()
        {
            var contacts = await _service.GetAllContactsAsync();
            return View(contacts);
        }

        // 2. API Phản hồi khách hàng
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reply([FromBody] ContactReplyDto dto)
        {
            var result = await _service.ReplyToContactAsync(dto);
            return Json(new { success = result.Success, message = result.Message });
        }

        // 3. API Xóa tin nhắn
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _service.DeleteContactAsync(id);
            return Json(new { success });
        }
    }
}