using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopQuanAo.Data;
using ShopQuanAo.Models;

namespace ShopQuanAo.Controllers
{
	[Authorize(Roles = "Admin")] // Đảm bảo chỉ Admin mới vào được
	public class ContactAdminController : Controller
	{
		private readonly ApplicationDbContext _context;

		public ContactAdminController(ApplicationDbContext context)
		{
			_context = context;
		}

		// 1. Trang danh sách phản hồi
		public async Task<IActionResult> Index()
		{
			var contacts = await _context.Contacts
				.OrderByDescending(c => c.CreatedDate)
				.ToListAsync();
			return View(contacts);
		}

		// 2. API Phản hồi khách hàng
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Reply(int id, string adminReply)
		{
			var contact = await _context.Contacts.FindAsync(id);
			if (contact == null) return Json(new { success = false, message = "Không tìm thấy tin nhắn." });

			if (!string.IsNullOrWhiteSpace(adminReply))
			{
				contact.AdminReply = adminReply;
				contact.IsRead = true;
				_context.Update(contact);
				await _context.SaveChangesAsync();
				return Json(new { success = true });
			}
			return Json(new { success = false, message = "Nội dung trống." });
		}

		// 3. API Xóa tin nhắn
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Delete(int id)
		{
			var contact = await _context.Contacts.FindAsync(id);
			if (contact == null) return Json(new { success = false });

			_context.Contacts.Remove(contact);
			await _context.SaveChangesAsync();
			return Json(new { success = true });
		}
	}
}