using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShopQuanAo.Models.BEAN.DTO;
using ShopQuanAo.BO;

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

		// 1. Trang danh sách phản hồi - Hiển thị toàn bộ liên hệ từ Database
		public async Task<IActionResult> Index()
		{
			var contacts = await _service.GetAllContactsAsync();
			return View(contacts);
		}

		// 2. API Phản hồi khách hàng
		[HttpPost]
		// Lưu ý: Nếu gửi bằng AJAX JSON [FromBody], đôi khi ValidateAntiForgeryToken sẽ gây lỗi 400 
		// nếu không truyền Token trong Header. Nếu bị lỗi đó, hãy tạm comment dòng dưới lại.
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Reply([FromBody] ContactsReplyDto dto)
		{
			// Kiểm tra dữ liệu đầu vào
			if (dto == null || string.IsNullOrWhiteSpace(dto.AdminReply))
			{
				return Json(new { success = false, message = "Vui lòng nhập nội dung phản hồi!" });
			}

			try
			{
				// Gọi Service xử lý lưu vào SQL và gửi Email
				var result = await _service.ReplyToContactAsync(dto);

				return Json(new
				{
					success = result.Success,
					message = result.Message
				});
			}
			catch (Exception ex)
			{
				return Json(new { success = false, message = "Lỗi hệ thống: " + ex.Message });
			}
		}

		// 3. API Xóa tin nhắn
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Delete(int id)
		{
			var success = await _service.DeleteContactAsync(id);
			return Json(new { success = success });
		}
	}
}