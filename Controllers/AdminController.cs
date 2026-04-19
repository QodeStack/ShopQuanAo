using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopQuanAo.Data;
using ShopQuanAo.BO;
using ShopQuanAo.Models.BEAN.DTO;
using ShopQuanAo.Models.BEAN.Entity;

namespace ShopQuanAo.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly AdminService _service;

        public AdminController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, AdminService service)
        {
            _context = context;
            _userManager = userManager;
            _service = service;
        }

        // --- Các trang giao diện (Views) ---
        public IActionResult Index()
        {
            ViewBag.RecentOrders = _context.Orders
                .Include(o => o.OrderDetails)
                .OrderByDescending(o => o.CreateTime)
                .Take(10)
                .ToList();
            return View();
        }

        public IActionResult Users() => View();

        public IActionResult Products()
        {
            ViewBag.Categories = _context.Categories.ToList();
            return View();
        }

        public async Task<IActionResult> Contacts()
        {
            var contacts = await _context.Contacts
                .OrderByDescending(c => c.CreatedDate)
                .ToListAsync();
            return View(contacts);
        }

        public IActionResult Orders() => View();
        public IActionResult Revenue() => View();

        // --- Các hàm lấy dữ liệu (API Endpoints) ---
        [HttpGet] public async Task<IActionResult> GetStats() => Json(await _service.GetStatsAsync());
        [HttpGet] public async Task<IActionResult> GetUsers() => Json(await _service.GetUsersAsync());
        [HttpGet] public async Task<IActionResult> GetProducts() => Json(await _service.GetAllProductsAsync());
        [HttpGet] public async Task<IActionResult> GetOrders() => Json(await _service.GetOrdersAsync());

        [HttpGet]
        public async Task<IActionResult> GetRevenueData(DateTime? startDate, DateTime? endDate)
        {
            var data = await _service.GetRevenueStatsAsync(startDate, endDate);
            return Ok(data);
        }

        // --- QUẢN LÝ SẢN PHẨM ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateProduct([FromBody] CreateProductWithSizesDto dto)
        {
            if (dto == null) return Json(new { success = false, message = "Dữ liệu không hợp lệ" });
            var res = await _service.CreateProductAsync(dto);
            return Json(new { success = res.Success, message = res.Message });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProduct([FromBody] UpdateProductDto dto)
        {
            try
            {
                if (dto == null) return Json(new { success = false, message = "Dữ liệu trống" });
                var res = await _service.UpdateProductAsync(dto);
                return Json(new { success = res.Success, message = res.Message });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProduct([FromBody] DeleteDto dto)
        {
            try
            {
                if (dto == null || string.IsNullOrEmpty(dto.Id))
                    return Json(new { success = false, message = "ID trống" });

                if (int.TryParse(dto.Id, out int productId))
                {
                    var res = await _service.DeleteProductAsync(productId);
                    if (res) return Json(new { success = true, message = "Đã xóa sản phẩm và các dữ liệu liên quan!" });
                    return Json(new { success = false, message = "Không tìm thấy sản phẩm hoặc lỗi SQL." });
                }
                return Json(new { success = false, message = "Định dạng ID không đúng" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        // --- QUẢN LÝ LIÊN HỆ ---
        [HttpPost]
        public async Task<IActionResult> ReplyContact([FromBody] ContactsReplyDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.AdminReply))
                return Json(new { success = false, message = "Vui lòng nhập nội dung phản hồi!" });

            var contact = await _context.Contacts.FindAsync(dto.Id);
            if (contact == null)
                return Json(new { success = false, message = "Không tìm thấy yêu cầu liên hệ!" });

            try
            {
                contact.AdminReply = dto.AdminReply;
                contact.IsRead = true;
                _context.Contacts.Update(contact);
                await _context.SaveChangesAsync();

                if (!string.IsNullOrEmpty(contact.Email))
                {
                    await _service.SendReplyContactEmailAsync(contact.Email, contact.FullName, dto.AdminReply);
                }
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi hệ thống: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteContact([FromBody] DeleteDto dto)
        {
            if (dto == null || string.IsNullOrEmpty(dto.Id))
                return Json(new { success = false, message = "ID không hợp lệ" });

            if (int.TryParse(dto.Id, out int contactId))
            {
                var res = await _service.DeleteContactAsync(contactId);
                return Json(new { success = res, message = res ? "Đã xóa liên hệ!" : "Lỗi khi xóa." });
            }
            return Json(new { success = false, message = "ID định dạng sai." });
        }

        // --- ORDER & USER KHÁC ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateOrderStatus([FromBody] UpdateOrderStatusDto dto)
        {
            var res = await _service.UpdateOrderStatusAsync(dto);
            return Json(new { success = res.Success, newStatus = res.NewStatus });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser([FromBody] DeleteDto dto)
        {
            var success = await _service.DeleteUserAsync(dto.Id, _userManager.GetUserId(User));
            return Json(new { success, message = success ? "Xóa thành công" : "Lỗi không thể xóa người dùng này!" });
        }

        // ==========================================================
        // --- KHU VỰC QUẢN LÝ KHUYẾN MÃI (SALE) ---
        // ==========================================================

        [HttpGet]
        public async Task<IActionResult> Sales([FromServices] ProductService productService, string search, int page = 1)
        {
            int pageSize = 10;
            var pagedData = await productService.GetPagedProductsAsync(null, search, page, pageSize, null, null, isSaleOnly: true);
            return View(pagedData.Products);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateSalePrice([FromBody] SaleRequestDto request)
        {
            // Kiểm tra SalePrice < 0 đã được chặn
            if (request == null || request.ProductId <= 0 || request.SalePrice < 0)
                return Json(new { success = false, message = "Dữ liệu không hợp lệ." });

            var res = await _service.UpdateSalePriceAsync(request.ProductId, request.SalePrice);
            return Json(new { success = res.Success, message = res.Message });
        }

        [HttpPost]
        public async Task<IActionResult> DisableSale([FromBody] SaleRequestDto request)
        {
            if (request == null || request.ProductId <= 0)
                return Json(new { success = false, message = "Dữ liệu không hợp lệ." });

            var res = await _service.DisableSaleAsync(request.ProductId);
            return Json(new { success = res.Success, message = res.Message });
        }

        [HttpGet]
        public async Task<IActionResult> SearchProducts([FromServices] ProductService productService, string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
                return Json(new List<object>());

            var results = await productService.SearchQuickAsync(keyword, null);
            return Json(results);
        }
        // Hiển thị giao diện Quản lý Voucher
        public async Task<IActionResult> Vouchers()
        {
            var vouchers = await _service.GetAllVouchersAsync();
            return View(vouchers);
        }

        // Thêm mã mới
        [HttpPost]
        public async Task<IActionResult> AddVoucher(Voucher voucher)
        {
            if (ModelState.IsValid)
            {
                await _service.AddVoucherAsync(voucher);
            }
            return RedirectToAction("Vouchers");
        }

        // Bật/Tắt mã
        [HttpPost]
        public async Task<IActionResult> ToggleVoucher(int id)
        {
            await _service.ToggleVoucherStatusAsync(id);
            return RedirectToAction("Vouchers");
        }

        // Xóa mã
        [HttpPost]
        public async Task<IActionResult> DeleteVoucher(int id)
        {
            await _service.DeleteVoucherAsync(id);
            return RedirectToAction("Vouchers");
        }
        [HttpPost]
        public async Task<IActionResult> EditVoucher(Voucher voucher)
        {
            if (ModelState.IsValid)
            {
                await _service.EditVoucherAsync(voucher);
            }
            return RedirectToAction("Vouchers");
        }

        [HttpPost]
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return Json(new { success = false, message = "Không có file nào được chọn." });

            try
            {
                // 1. Tạo đường dẫn thư mục lưu trữ
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "products");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                // 2. Tạo tên file duy nhất để tránh trùng lặp
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                var filePath = Path.Combine(uploadsFolder, fileName);

                // 3. Lưu file vào thư mục
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // 4. Trả về đường dẫn tương đối để lưu vào Database
                var url = "/images/products/" + fileName;
                return Json(new { success = true, url = url });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi hệ thống: " + ex.Message });
            }
        }

    }

    // --- DTO: BẠN CÓ THỂ ĐỂ TẠM Ở ĐÂY HOẶC CẮT SANG FILE RIÊNG (SaleRequestDto.cs) ---
    
}