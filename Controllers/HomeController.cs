using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopQuanAo.BO;
using ShopQuanAo.Data;
using ShopQuanAo.Models;
using ShopQuanAo.Models.BEAN.DTO;
using ShopQuanAo.Models.BEAN.Entity;
using System.Diagnostics;

namespace ShopQuanAo.Controllers
{
    public class HomeController : Controller
    {
        private readonly HomeService _homeService;
        private readonly ApplicationDbContext _context;
        public HomeController(HomeService homeService, ApplicationDbContext context)  // ← SỬA
        {
            _homeService = homeService;
            _context = context;  // ← THÊM
        }
        // Các trang tĩnh
        public async Task<IActionResult> Index()
        {
            var saleProducts = await _homeService.GetSaleProductsAsync();
            ViewBag.SaleProducts = saleProducts;

            var newArrivals = await _homeService.GetNewArrivalsAsync();
            ViewBag.NewArrivals = newArrivals;

            var bestSellers = await _homeService.GetBestSellersAsync();
            ViewBag.BestSellers = bestSellers;

            ViewBag.AoThun = await _homeService.GetProductsByCategoryAsync("Áo Thun");
            ViewBag.AoSomi = await _homeService.GetProductsByCategoryAsync("Áo Sơ Mi");
            ViewBag.QuanJean = await _homeService.GetProductsByCategoryAsync("Quần Jean");
            ViewBag.QuanTay = await _homeService.GetProductsByCategoryAsync("Quần Tây");

            // ← THÊM MỚI: gom tất cả productId để lấy reviews 1 lần duy nhất
            var allProducts = new List<ShopQuanAo.Models.BEAN.Entity.Product>();
            if (saleProducts != null) allProducts.AddRange(saleProducts);
            if (newArrivals != null) allProducts.AddRange(newArrivals);
            if (bestSellers != null) allProducts.AddRange(bestSellers);
            if (ViewBag.AoThun != null) allProducts.AddRange((List<ShopQuanAo.Models.BEAN.Entity.Product>)ViewBag.AoThun);
            if (ViewBag.AoSomi != null) allProducts.AddRange((List<ShopQuanAo.Models.BEAN.Entity.Product>)ViewBag.AoSomi);
            if (ViewBag.QuanJean != null) allProducts.AddRange((List<ShopQuanAo.Models.BEAN.Entity.Product>)ViewBag.QuanJean);
            if (ViewBag.QuanTay != null) allProducts.AddRange((List<ShopQuanAo.Models.BEAN.Entity.Product>)ViewBag.QuanTay);

            var allIds = allProducts.Select(p => p.Id).Distinct().ToList();
            var reviews = await _context.ProductReviews
                .Where(r => allIds.Contains(r.ProductId))
                .ToListAsync();

            var allReviews = allIds.ToDictionary(
                id => id,
                id => reviews.Where(r => r.ProductId == id).ToList()
            );
            ViewBag.AllReviews = allReviews;

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