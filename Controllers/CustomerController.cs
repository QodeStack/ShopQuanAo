using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopQuanAo.Models.BEAN.Entity;
using ShopQuanAo.BO;
using ShopQuanAo.ViewModels;

namespace ShopQuanAo.Controllers
{
    [Authorize]
    public class CustomerController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly CustomerService _customerService;
        private readonly ShopQuanAo.Data.ApplicationDbContext _context;

        public CustomerController(
            UserManager<ApplicationUser> userManager,
            CustomerService customerService,
            ShopQuanAo.Data.ApplicationDbContext context) // Inject context vào đây
        {
            _userManager = userManager;
            _customerService = customerService;
            _context = context;
        }

        public async Task<IActionResult> Index(string status = "", int page = 1)
        {
            var userId = _userManager.GetUserId(User);
            var result = await _customerService.GetOrdersAsync(userId, status, page);

            // Lấy danh sách ID các sản phẩm mà người dùng này đã đánh giá
            var reviewedProductIds = _context.ProductReviews
                .Where(r => r.UserId == userId)
                .Select(r => r.ProductId)
                .ToList();

            var vm = new CustomerOrderViewModel
            {
                Orders = result.Orders,
                CurrentPage = result.CurrentPage,
                TotalPages = result.TotalPages,
                CurrentStatus = result.CurrentStatus
            };

            // Truyền danh sách ID đã đánh giá qua ViewBag
            ViewBag.ReviewedProductIds = reviewedProductIds;

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int orderId)
        {
            var result = await _customerService.CancelOrderAsync(_userManager.GetUserId(User), orderId);
            if (result.Success) TempData["Message"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmReceived(int orderId)
        {
            var result = await _customerService.ConfirmReceivedAsync(_userManager.GetUserId(User), orderId);
            if (result.Success) TempData["Message"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

    }
}