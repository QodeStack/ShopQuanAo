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
        public IActionResult Index() => View();
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