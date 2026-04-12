using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using MimeKit;
using MailKit.Net.Smtp;
using ShopQuanAo.Models.Entity;

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

		// SỬA: Thêm dấu ? để hệ thống không bắt buộc nhập lúc bấm Đăng ký bước 1
		[BindProperty]
		public string? EmailPending { get; set; }

		// SỬA: Thêm dấu ? để hệ thống không bắt buộc nhập lúc bấm Đăng ký bước 1
		[BindProperty]
		public string? OtpInput { get; set; }

		public string ReturnUrl { get; set; }

		public IList<AuthenticationScheme> ExternalLogins { get; set; }

		public class InputModel
		{
			[Required(ErrorMessage = "Vui lòng nhập Email.")]
			[EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
			public string Email { get; set; }

			[Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
			[StringLength(100, ErrorMessage = "Mật khẩu phải từ {2} đến {1} ký tự.", MinimumLength = 6)]
			[DataType(DataType.Password)]
			public string Password { get; set; }

			[DataType(DataType.Password)]
			[Compare("Password", ErrorMessage = "Mật khẩu xác nhận không khớp.")]
			public string ConfirmPassword { get; set; }
		}

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

			// SỬA: Xóa các lỗi validation của OTP và EmailPending khi đang ở bước Đăng ký ban đầu
			ModelState.Remove("OtpInput");
			ModelState.Remove("EmailPending");

			if (ModelState.IsValid)
			{
				var user = CreateUser();
				await _userStore.SetUserNameAsync(user, Input.Email, CancellationToken.None);
				await _emailStore.SetEmailAsync(user, Input.Email, CancellationToken.None);

				var result = await _userManager.CreateAsync(user, Input.Password);

				if (result.Succeeded)
				{
					string otpCode = new Random().Next(100000, 999999).ToString();
					user.OTPCode = otpCode;
					user.OTPExpiry = DateTime.Now.AddMinutes(5);
					user.EmailConfirmed = false;

					await _userManager.UpdateAsync(user);
					await _userManager.AddToRoleAsync(user, "User");

					try
					{
						await SendEmailOTP(Input.Email, otpCode);
						EmailPending = Input.Email;
						ViewData["ShowOTP"] = true;
						return Page();
					}
					catch (Exception ex)
					{
						ModelState.AddModelError(string.Empty, "Lỗi gửi mail: " + ex.Message);
					}
				}

				foreach (var error in result.Errors)
				{
					ModelState.AddModelError(string.Empty, error.Description);
				}
			}

			return Page();
		}

		public async Task<IActionResult> OnPostVerifyOTPAsync(string returnUrl = null)
		{
			returnUrl ??= Url.Content("~/");

			if (string.IsNullOrEmpty(EmailPending))
			{
				ModelState.AddModelError(string.Empty, "Phiên làm việc đã hết hạn, vui lòng đăng ký lại.");
				ViewData["ShowOTP"] = false;
				return Page();
			}

			var user = await _userManager.FindByEmailAsync(EmailPending);

			if (user != null && user.OTPCode == OtpInput && user.OTPExpiry > DateTime.Now)
			{
				user.EmailConfirmed = true;
				user.OTPCode = null;
				user.OTPExpiry = null;

				var result = await _userManager.UpdateAsync(user);
				if (result.Succeeded)
				{
					TempData["StatusMessage"] = "Chúc mừng! Bạn đã đăng ký thành công tài khoản MenShop. Hãy đăng nhập ngay.";
					return RedirectToPage("Login");
				}
			}

			ModelState.AddModelError(string.Empty, "Mã OTP không chính xác hoặc đã hết hạn.");
			ViewData["ShowOTP"] = true;
			return Page();
		}

		private ApplicationUser CreateUser()
		{
			try { return Activator.CreateInstance<ApplicationUser>(); }
			catch { throw new InvalidOperationException($"Không thể tạo instance của '{nameof(ApplicationUser)}'."); }
		}

		private IUserEmailStore<ApplicationUser> GetEmailStore() => (IUserEmailStore<ApplicationUser>)_userStore;
	}
}