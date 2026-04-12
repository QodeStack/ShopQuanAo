using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ShopQuanAo.Models.Entity;
using ShopQuanAo.ViewModels;
using ShopQuanAo.Services;

namespace ShopQuanAo.Controllers
{
    [Authorize]
    public class CustomerController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly CustomerService _customerService;

        public CustomerController(UserManager<ApplicationUser> userManager, CustomerService customerService)
        {
            _userManager = userManager;
            _customerService = customerService;
        }

        public async Task<IActionResult> Index(string status = "", int page = 1)
        {
            var result = await _customerService.GetOrdersAsync(_userManager.GetUserId(User), status, page);

            var vm = new CustomerOrderViewModel
            {
                Orders = result.Orders,
                CurrentPage = result.CurrentPage,
                TotalPages = result.TotalPages,
                CurrentStatus = result.CurrentStatus
            };

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