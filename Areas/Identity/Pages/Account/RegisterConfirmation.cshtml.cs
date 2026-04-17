// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using ShopQuanAo.Models.BEAN.Entity;

namespace ShopQuanAo.Areas.Identity.Pages.Account
{
	[AllowAnonymous]
	public class RegisterConfirmationModel : PageModel
	{
		private readonly UserManager<ApplicationUser> _userManager;
		private readonly SignInManager<ApplicationUser> _signInManager;

		public RegisterConfirmationModel(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
		{
			_userManager = userManager;
			_signInManager = signInManager;
		}

		public string Email { get; set; }

		// Thuộc tính để nhận mã OTP từ giao diện (ô input)
		[BindProperty]
		public string OtpInput { get; set; }

		public async Task<IActionResult> OnGetAsync(string email, string returnUrl = null)
		{
			if (email == null)
			{
				return RedirectToPage("/Index");
			}

			var user = await _userManager.FindByEmailAsync(email);
			if (user == null)
			{
				return NotFound($"Không tìm thấy người dùng với email '{email}'.");
			}

			Email = email;
			return Page();
		}

		// Hàm xử lý khi khách nhấn nút "Xác thực"
		public async Task<IActionResult> OnPostAsync(string email)
		{
			if (string.IsNullOrEmpty(OtpInput))
			{
				ModelState.AddModelError(string.Empty, "Vui lòng nhập mã OTP.");
				Email = email;
				return Page();
			}

			var user = await _userManager.FindByEmailAsync(email);
			if (user == null) return NotFound();

			// KIỂM TRA MÃ OTP
			if (user.OTPCode == OtpInput && user.OTPExpiry > DateTime.Now)
			{
				// Nếu đúng: Kích hoạt tài khoản
				user.EmailConfirmed = true;
				user.OTPCode = null; // Xóa mã cũ để bảo mật
				await _userManager.UpdateAsync(user);

				// Đăng nhập luôn cho khách
				await _signInManager.SignInAsync(user, isPersistent: false);

				return RedirectToPage("/Index");
			}

			// Nếu sai hoặc hết hạn
			ModelState.AddModelError(string.Empty, "Mã OTP không chính xác hoặc đã hết hạn (5 phút).");
			Email = email;
			return Page();
		}
	}
}