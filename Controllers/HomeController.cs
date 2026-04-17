using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShopQuanAo.Models;
using ShopQuanAo.Models.DTO;
using ShopQuanAo.Models.Entity;
using ShopQuanAo.Services;
using System.Diagnostics;

namespace ShopQuanAo.Controllers
{
	public class HomeController : Controller
	{
		private readonly HomeService _homeService;

		public HomeController(HomeService homeService)
		{
			_homeService = homeService;
		}

		// SỬA TẠI ĐÂY: Biến Index thành async để lấy dữ liệu sản phẩm
		public async Task<IActionResult> Index()
		{
			// Lấy danh sách sản phẩm Sale từ Service
			var saleProducts = await _homeService.GetSaleProductsAsync();

			// Cách 1: Truyền trực tiếp qua Model (Nếu Index.cshtml dùng @model IEnumerable<ShopQuanAo.Models.Entity.Products>)
			return View(saleProducts);

			// Cách 2: Nếu bạn muốn dùng ViewBag để dành Model cho cái khác
			// ViewBag.SaleProducts = saleProducts;
			// return View();
		}

		public IActionResult AboutUs() => View();

		public IActionResult Sale()
		{
			return RedirectToAction("Sale", "Product");
		}

		public IActionResult Contact() => View();

		[Authorize]
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> SendContact(ContactsFormDto model)
		{
			if (!ModelState.IsValid)
			{
				return View("Contact", model);
			}

			var success = await _homeService.SaveContactAsync(model);
			if (success)
			{
				TempData["SuccessMessage"] = "Gửi tin nhắn thành công! Admin sẽ phản hồi bạn sớm nhất.";
				return RedirectToAction("Contact");
			}

			ModelState.AddModelError("", "Có lỗi xảy ra khi gửi tin nhắn.");
			return View("Contact", model);
		}

		[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
		public IActionResult Error()
		{
			return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
		}
	}
}