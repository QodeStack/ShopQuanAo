using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShopQuanAo.Models;
using ShopQuanAo.Models.BEAN.DTO;
using ShopQuanAo.Models.BEAN.Entity;
using ShopQuanAo.BO;
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

		// Các trang tĩnh
		public async Task<IActionResult> Index()
		{
			// 1. Lấy sản phẩm Sale (Giữ nguyên)
			var saleProducts = await _homeService.GetSaleProductsAsync();
			ViewBag.SaleProducts = saleProducts;

			// 2. Lấy sản phẩm mới (Thêm mới)
			var newArrivals = await _homeService.GetNewArrivalsAsync();
			ViewBag.NewArrivals = newArrivals;

			// 3. Lấy sản phẩm bán chạy (Thêm mới)
			var bestSellers = await _homeService.GetBestSellersAsync();
			ViewBag.BestSellers = bestSellers;

			// --- PHẦN SỬA MỚI: Lấy dữ liệu cho 4 Showcase danh mục ---
			// Lưu ý: Tên danh mục truyền vào phải khớp chính xác với DB
			ViewBag.AoThun = await _homeService.GetProductsByCategoryAsync("Áo Thun");
			ViewBag.AoSomi = await _homeService.GetProductsByCategoryAsync("Áo Sơ Mi");
			ViewBag.QuanJean = await _homeService.GetProductsByCategoryAsync("Quần Jean");
			ViewBag.QuanTay = await _homeService.GetProductsByCategoryAsync("Quần Tây");

			return View();
		}
		public IActionResult AboutUs() => View();
        public IActionResult Sale()
        {
            // Lệnh này sẽ đá người dùng từ Home/Sale sang Product/Sale
            return RedirectToAction("Sale", "Product");
        }
        public IActionResult Contact() => View();

        // Xử lý gửi liên hệ
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        // SỬA TẠI ĐÂY: Đổi ContactFormDto thành ContactsFormDto cho khớp với file DTO của bạn
        public async Task<IActionResult> SendContact(ContactsFormDto model)
        {
            if (!ModelState.IsValid)
            {
                return View("Contact", model);
            }

            // Gọi Service để lưu vào SQL
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