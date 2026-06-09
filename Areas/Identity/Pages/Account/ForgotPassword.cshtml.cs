// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using MimeKit;
using MailKit.Net.Smtp;
using ShopQuanAo.Models.BEAN.Entity;

namespace ShopQuanAo.Areas.Identity.Pages.Account
{
    public class ForgotPasswordModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public ForgotPasswordModel(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        private async Task SendPasswordResetEmailAsync(string email, string resetLink)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("MenShop Security", "leythien2508@gmail.com"));
            message.To.Add(new MailboxAddress("", email));
            message.Subject = "Đặt lại mật khẩu MenShop";

            message.Body = new TextPart("html")
            {
                Text = $@"
                    <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                        <h2 style='color: #333;'>Đặt lại mật khẩu MenShop</h2>
                        <p>Chúng tôi nhận được yêu cầu đặt lại mật khẩu cho tài khoản của bạn.</p>
                        <p>Vui lòng nhấp vào liên kết dưới đây để đặt lại mật khẩu:</p>
                        <p><a href='{resetLink}' style='display: inline-block; padding: 10px 20px; background-color: #ee4d2d; color: white; text-decoration: none; border-radius: 5px;'>Đặt lại mật khẩu</a></p>
                        <p style='color: #666; font-size: 12px;'>Liên kết này sẽ hết hạn sau 24 giờ.</p>
                        <p style='color: #666; font-size: 12px;'>Nếu bạn không yêu cầu điều này, vui lòng bỏ qua email này.</p>
                    </div>
                "
            };

            using (var client = new SmtpClient())
            {
                await client.ConnectAsync("smtp.gmail.com", 587, MailKit.Security.SecureSocketOptions.StartTls);
                await client.AuthenticateAsync("leythien2508@gmail.com", "hszr hbjw vamm twxa");
                await client.SendAsync(message);
                await client.DisconnectAsync(true);
            }
        }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [BindProperty]
        public InputModel Input { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public class InputModel
        {
            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required]
            [EmailAddress]
            public string Email { get; set; }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(Input.Email);
                if (user == null || !(await _userManager.IsEmailConfirmedAsync(user)))
                {
                    // Không tiết lộ rằng user không tồn tại hoặc email chưa được xác thực
                    return RedirectToPage("./ForgotPasswordConfirmation");
                }

                try
                {
                    // Tạo token đặt lại mật khẩu
                    var code = await _userManager.GeneratePasswordResetTokenAsync(user);
                    code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

                    var callbackUrl = Url.Page(
                        "/Account/ResetPassword",
                        pageHandler: null,
                        values: new { area = "Identity", code },
                        protocol: Request.Scheme);

                    // Gửi email với link đặt lại mật khẩu
                    await SendPasswordResetEmailAsync(Input.Email, callbackUrl);

                    return RedirectToPage("./ForgotPasswordConfirmation");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(string.Empty, "Lỗi gửi email: " + ex.Message);
                }
            }

            return Page();
        }
    }
}
