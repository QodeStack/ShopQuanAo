using Microsoft.AspNetCore.Identity;
using MailKit.Net.Smtp;
using MimeKit;
using ShopQuanAo.Models.BEAN.DTO;
using ShopQuanAo.Models.BEAN.Entity;
using ShopQuanAo.DAO;
using Microsoft.EntityFrameworkCore;
namespace ShopQuanAo.BO
{
    public class AdminService
    {
        private readonly AdminDAO _adminDAO;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminService(AdminDAO adminDAO, UserManager<ApplicationUser> userManager)
        {
            _adminDAO = adminDAO;
            _userManager = userManager;
        }

        #region Helper Methods (Email & OTP)
        public string GenerateOTP() => new Random().Next(100000, 999999).ToString();

        public async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("MenShop System", "leythien2508@gmail.com"));
            message.To.Add(new MailboxAddress("", toEmail));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder { HtmlBody = htmlBody };
            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync("smtp.gmail.com", 587, MailKit.Security.SecureSocketOptions.StartTls);
            await client.AuthenticateAsync("leythien2508@gmail.com", "hszr hbjw vamm twxa");
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
        #endregion

        #region Dashboard & Stats (Doanh thu nâng cao)
        public async Task<object> GetRevenueStatsAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            var now = DateTime.Now;

            var start = startDate ?? new DateTime(now.Year, now.Month, 1);
            var end = endDate ?? now;
            var endOfPeriod = end.Date.AddDays(1).AddTicks(-1);

            // Lấy dữ liệu qua DAO
            var paidOrders = await _adminDAO.GetPaidOrderDetailsAsync(start, endOfPeriod);
            var categories = await _adminDAO.GetAllCategoriesAsync();

            var totalRevenue = paidOrders.Sum(od => (double)od.UnitPrice * od.Quantity);
            var totalOrders = await _adminDAO.CountValidOrdersAsync(start, endOfPeriod);
            var totalItemsSold = paidOrders.Sum(od => od.Quantity);
            var totalUsers = await _userManager.Users.CountAsync();

            var categoryData = paidOrders
                .GroupBy(od => od.Product.CategoryId)
                .Select(g => new {
                    name = categories.FirstOrDefault(c => c.Id == g.Key)?.CategoryName ?? "Khác",
                    value = g.Sum(x => x.Quantity * x.UnitPrice)
                }).ToList();

            var topProducts = paidOrders
                .GroupBy(od => new { od.ProductId, od.Product.ProductName, od.Product.Image })
                .Select(g => new {
                    productName = g.Key.ProductName,
                    image = g.Key.Image,
                    soldCount = g.Sum(x => x.Quantity),
                    revenue = g.Sum(x => x.Quantity * x.UnitPrice)
                })
                .OrderByDescending(x => x.soldCount)
                .Take(5)
                .ToList();

            var dailyData = paidOrders
                .GroupBy(od => od.Order.CreateTime.Date)
                .Select(g => new {
                    date = g.Key.ToString("dd/MM"),
                    revenue = g.Sum(x => x.Quantity * x.UnitPrice)
                })
                .OrderBy(x => x.date)
                .ToList();

            return new
            {
                metrics = new { totalRevenue, totalOrders, totalItemsSold, totalUsers },
                categoryData,
                topProducts,
                dailyData,
                period = new { start = start.ToString("dd/MM/yyyy"), end = end.ToString("dd/MM/yyyy") }
            };
        }

        public async Task<object> GetStatsAsync()
        {
            var now = DateTime.Now;
            var startOfMonth = new DateTime(now.Year, now.Month, 1);

            var paidOrders = await _adminDAO.GetPaidOrderDetailsAsync(startOfMonth, now);
            var revenue = paidOrders.Sum(od => (double?)(od.UnitPrice * od.Quantity)) ?? 0;

            return new
            {
                revenue,
                orders = await _adminDAO.CountValidOrdersAsync(startOfMonth, now),
                users = await _userManager.Users.CountAsync(),
                products = await _adminDAO.CountDistinctProductsAsync()
            };
        }
        #endregion

        #region User Management
        public async Task<List<object>> GetUsersAsync()
        {
            var users = await _userManager.Users.ToListAsync();
            var result = new List<object>();
            foreach (var u in users)
            {
                var roles = await _userManager.GetRolesAsync(u);
                result.Add(new { id = u.Id, email = u.Email, userName = u.UserName, emailConfirmed = u.EmailConfirmed, twoFactorEnabled = u.TwoFactorEnabled, roles });
            }
            return result;
        }

        public async Task<(bool Success, string Message)> CreateUserAsync(CreateUserDto dto)
        {
            var user = new ApplicationUser
            {
                UserName = dto.Email,
                Email = dto.Email,
                EmailConfirmed = false,
                OTPCode = GenerateOTP(),
                OTPExpiry = DateTime.Now.AddMinutes(5)
            };
            var result = await _userManager.CreateAsync(user, dto.Password);
            if (!result.Succeeded) return (false, string.Join(", ", result.Errors.Select(e => e.Description)));

            await SendEmailAsync(user.Email, "Mã xác thực MenShop", $"Mã OTP của bạn là: <b>{user.OTPCode}</b>");
            if (!string.IsNullOrEmpty(dto.Role)) await _userManager.AddToRoleAsync(user, dto.Role);
            return (true, "Đã gửi mã OTP vào email!");
        }

        public async Task<bool> DeleteUserAsync(string id, string currentUserId)
        {
            if (id == currentUserId) return false;
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return false;

            await _adminDAO.RemoveCartsByUserIdAsync(id);
            await _userManager.DeleteAsync(user);
            return true;
        }
        #endregion

        #region Product & Size Management
        public async Task<object> GetAllProductsAsync()
        {
            var products = await _adminDAO.GetAllProductsWithRelationsAsync();
            return products.Select(p => new {
                id = p.Id,
                productName = p.ProductName,
                brandName = p.BrandName,
                price = p.Price,
                image = p.Image,
                categoryId = p.CategoryId,
                categoryName = p.Category != null ? p.Category.CategoryName : "N/A",
                productSizes = p.ProductSizes.Select(ps => new { id = ps.Id, sizeName = ps.Size.SizeName, quantity = ps.Quantity }).ToList()
            }).ToList();
        }

        public async Task<(bool Success, string Message)> CreateProductAsync(CreateProductWithSizesDto dto)
        {
            if (await _adminDAO.IsProductExistAsync(dto.ProductName))
                return (false, "Sản phẩm đã tồn tại.");

            var product = new Product { ProductName = dto.ProductName, BrandName = dto.BrandName ?? "MenShop", Price = dto.Price, Image = dto.Image, CategoryId = dto.CategoryId };
            await _adminDAO.AddProductAsync(product);

            if (dto.Sizes != null)
            {
                foreach (var s in dto.Sizes)
                {
                    var size = await _adminDAO.GetSizeByNameAsync(s.SizeName.ToUpper());
                    if (size == null)
                    {
                        size = new Size { SizeName = s.SizeName.ToUpper() };
                        await _adminDAO.AddSizeAsync(size);
                    }

                    await _adminDAO.AddProductSizeAsync(new ProductSize { ProductId = product.Id, SizeId = size.Id, Quantity = s.Quantity });
                }
            }
            return (true, "Thành công");
        }

        public async Task<(bool Success, string Message)> UpdateProductAsync(UpdateProductDto dto)
        {
            var product = await _adminDAO.GetProductByIdAsync(dto.Id);
            if (product == null) return (false, "Không tìm thấy sản phẩm.");

            try
            {
                product.ProductName = dto.ProductName;
                product.Price = dto.Price;
                product.BrandName = dto.BrandName ?? "MenShop";
                product.CategoryId = dto.CategoryId;
                if (!string.IsNullOrEmpty(dto.Image)) product.Image = dto.Image;

                await _adminDAO.RemoveProductSizesAsync(product.ProductSizes);

                if (dto.Sizes != null)
                {
                    foreach (var s in dto.Sizes)
                    {
                        var size = await _adminDAO.GetSizeByNameAsync(s.SizeName.ToUpper());
                        if (size == null)
                        {
                            size = new Size { SizeName = s.SizeName.ToUpper() };
                            await _adminDAO.AddSizeAsync(size);
                        }

                        await _adminDAO.AddProductSizeAsync(new ProductSize { ProductId = product.Id, SizeId = size.Id, Quantity = s.Quantity });
                    }
                }

                await _adminDAO.SaveChangesAsync();
                return (true, "Thành công");
            }
            catch (Exception ex)
            {
                return (false, "Lỗi SQL: " + (ex.InnerException?.Message ?? ex.Message));
            }
        }

        public async Task<bool> DeleteProductAsync(int id)
        {
            return await _adminDAO.DeleteProductDependenciesAsync(id);
        }
        #endregion

        #region Order & Contact Management
        public async Task<List<object>> GetOrdersAsync()
        {
            var orders = await _adminDAO.GetAllOrdersWithRelationsAsync();
            return orders.Select(o => new {
                id = o.Id,
                name = o.Name,
                email = o.Email,
                paymentMethod = o.PaymentMethod,
                isPaid = o.IsPaid,
                createTime = o.CreateTime,
                statusName = o.OrderStatus.StatusName,
                orderDetails = o.OrderDetails.Select(d => new { d.Id, d.Quantity, d.UnitPrice, productName = d.Product.ProductName }).ToList()
            }).Cast<object>().ToList();
        }

        public async Task<(bool Success, string NewStatus)> UpdateOrderStatusAsync(UpdateOrderStatusDto dto)
        {
            var order = await _adminDAO.GetOrderByIdAsync(dto.OrderId);
            if (order == null) return (false, "");

            string? nextStatusName = dto.Action switch
            {
                "confirm" when order.OrderStatus.StatusName == "Chờ xác nhận" => "Đang xử lý",
                "ship" when order.OrderStatus.StatusName == "Đang xử lý" => "Đang giao hàng",
                "cancel" when order.OrderStatus.StatusName == "Chờ xác nhận" => "Đã hủy",
                _ => null
            };

            if (nextStatusName == null) return (false, "");

            var status = await _adminDAO.GetOrderStatusByNameAsync(nextStatusName);
            order.OrderStatusId = status.Id;
            await _adminDAO.SaveChangesAsync();

            return (true, nextStatusName);
        }

        public async Task<bool> DeleteContactAsync(int id)
        {
            return await _adminDAO.DeleteContactAsync(id);
        }

        public async Task SendReplyContactEmailAsync(string toEmail, string fullName, string replyMessage)
        {
            string finalMessage = string.IsNullOrWhiteSpace(replyMessage) ? "Cảm ơn bạn đã liên hệ với chúng tôi." : replyMessage;
            string subject = "Phản hồi từ Ban quản trị MenShop";
            string body = $@"
                <div style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
                    <h2 style='color: #78ae2d;'>Chào {fullName},</h2>
                    <p>Chúng tôi đã nhận được yêu cầu của bạn. Admin MenShop xin phản hồi nội dung như sau:</p>
                    <div style='background: #f9f9f9; padding: 20px; border-left: 5px solid #78ae2d; margin: 20px 0; font-style: italic;'>
                        ""{finalMessage}""
                    </div>
                    <p>Trân trọng,<br/><b>Đội ngũ MenShop</b></p>
                </div>";
            await SendEmailAsync(toEmail, subject, body);
        }
        #endregion
    }
}