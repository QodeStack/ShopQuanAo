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

        // Các trang tĩnh
        public IActionResult Index() => View();
        public IActionResult AboutUs() => View();
        public IActionResult Sale() => View();
        public IActionResult Contact() => View();

        // Xử lý gửi liên hệ
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendContact(ContactFormDto model)
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