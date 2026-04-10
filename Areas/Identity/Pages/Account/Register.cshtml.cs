// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using ShopQuanAo.Const;
using ShopQuanAo.Models; // Quan trọng: Để nhận diện ApplicationUser
using MimeKit;
using MailKit.Net.Smtp;

namespace ShopQuanAo.Areas.Identity.Pages.Account
{
	public class RegisterModel : PageModel
	{
		private readonly SignInManager<ApplicationUser> _signInManager;
		private readonly UserManager<ApplicationUser> _userManager;
		private readonly IUserStore<ApplicationUser> _userStore;
		private readonly IUserEmailStore<ApplicationUser> _emailStore;
		private readonly ILogger<RegisterModel> _logger;

		public RegisterModel(
			UserManager<ApplicationUser> userManager,
			IUserStore<ApplicationUser> userStore,
			SignInManager<ApplicationUser> signInManager,
			ILogger<RegisterModel> logger)
		{
			_userManager = userManager;
			_userStore = userStore;
			_emailStore = GetEmailStore();
			_signInManager = signInManager;
			_logger = logger;
		}

		[BindProperty]
		public InputModel Input { get; set; }

		public string ReturnUrl { get; set; }

		public IList<AuthenticationScheme> ExternalLogins { get; set; }

		public class InputModel
		{
			[Required]
			[EmailAddress]
			[Display(Name = "Email")]
			public string Email { get; set; }

			[Required]
			[StringLength(100, ErrorMessage = "Mật khẩu phải từ {2} đến {1} ký tự.", MinimumLength = 6)]
			[DataType(DataType.Password)]
			[Display(Name = "Password")]
			public string Password { get; set; }

			[DataType(DataType.Password)]
			[Display(Name = "Confirm password")]
			[Compare("Password", ErrorMessage = "Mật khẩu xác nhận không khớp.")]
			public string ConfirmPassword { get; set; }
		}

		// --- Hàm hỗ trợ gửi OTP bằng Mail của Quốc ---
		private async Task SendEmailOTP(string email, string otp)
		{
			var message = new MimeMessage();
			message.From.Add(new MailboxAddress("MenShop Security", "leythien2508@gmail.com"));
			message.To.Add(new MailboxAddress("", email));
			message.Subject = "Mã xác thực đăng ký MenShop";

			message.Body = new TextPart("plain")
			{
				Text = $"Chào bạn, mã xác thực (OTP) để hoàn tất đăng ký MenShop của bạn là: {otp}. Mã có hiệu lực trong 5 phút."
			};

			using (var client = new SmtpClient())
			{
				await client.ConnectAsync("smtp.gmail.com", 587, MailKit.Security.SecureSocketOptions.StartTls);
				await client.AuthenticateAsync("leythien2508@gmail.com", "hszr hbjw vamm twxa");
				await client.SendAsync(message);
				await client.DisconnectAsync(true);
			}
		}

		public async Task OnGetAsync(string returnUrl = null)
		{
			ReturnUrl = returnUrl;
			ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
		}

		public async Task<IActionResult> OnPostAsync(string returnUrl = null)
		{
			returnUrl ??= Url.Content("~/");
			ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
			if (ModelState.IsValid)
			{
				var user = CreateUser();

				await _userStore.SetUserNameAsync(user, Input.Email, CancellationToken.None);
				await _emailStore.SetEmailAsync(user, Input.Email, CancellationToken.None);

				// SINH MÃ OTP VÀ LƯU VÀO CỘT MỚI
				string otpCode = new Random().Next(100000, 999999).ToString();
				user.OTPCode = otpCode;
				user.OTPExpiry = DateTime.Now.AddMinutes(5);
				user.EmailConfirmed = false; // Bắt buộc phải xác nhận OTP mới được tính là xong

				var result = await _userManager.CreateAsync(user, Input.Password);

				if (result.Succeeded)
				{
					await _userManager.AddToRoleAsync(user, "User");
					_logger.LogInformation("User created a new account.");

					// GỬI MAIL OTP CHO KHÁCH
					try
					{
						await SendEmailOTP(Input.Email, otpCode);
					}
					catch (Exception ex)
					{
						_logger.LogError($"Lỗi gửi mail: {ex.Message}");
					}

					// CHUYỂN HƯỚNG SANG TRANG XÁC NHẬN (Của Identity mặc định)
					return RedirectToPage("RegisterConfirmation", new { email = Input.Email, returnUrl = returnUrl });
				}
				foreach (var error in result.Errors)
				{
					ModelState.AddModelError(string.Empty, error.Description);
				}
			}

			return Page();
		}

		private ApplicationUser CreateUser()
		{
			try
			{
				return Activator.CreateInstance<ApplicationUser>();
			}
			catch
			{
				throw new InvalidOperationException($"Không thể tạo instance của '{nameof(ApplicationUser)}'.");
			}
		}

		private IUserEmailStore<ApplicationUser> GetEmailStore()
		{
			return (IUserEmailStore<ApplicationUser>)_userStore;
		}
	}
}