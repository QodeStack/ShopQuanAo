using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopQuanAo.Data;
using ShopQuanAo.Models;
using System.Diagnostics;

namespace ShopQuanAo.Controllers
{
	public class HomeController : Controller
	{
		private readonly ApplicationDbContext _context;

		// Inject Database Context để sử dụng
		public HomeController(ApplicationDbContext context)
		{
			_context = context;
		}

		public IActionResult Index()
		{
			return View();
		}

		public IActionResult AboutUs()
		{
			return View();
		}

		public IActionResult Sale()
		{
			return View();
		}

		public IActionResult Contact()
		{
			return View();
		}

		// Action xử lý khi khách hàng gửi thông tin liên hệ
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> SendContact(Contacts model)
		{
			if (ModelState.IsValid)
			{
				// Mặc định ngày tạo là hiện tại
				model.CreatedDate = DateTime.Now;
				model.IsRead = false;

				_context.Contacts.Add(model);
				await _context.SaveChangesAsync();

				// Thông báo thành công cho người dùng
				TempData["SuccessMessage"] = "Gửi tin nhắn thành công! Admin sẽ phản hồi bạn sớm nhất có thể.";
				return RedirectToAction("Contact");
			}

			// Nếu dữ liệu không hợp lệ, trả về view kèm lỗi
			return View("Contact", model);
		}

		[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
		public IActionResult Error()
		{
			return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
		}
	}
}