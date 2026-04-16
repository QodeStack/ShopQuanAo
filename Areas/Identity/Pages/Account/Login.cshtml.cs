// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using ShopQuanAo.Models.BEAN.Entity; // THÊM DÒNG NÀY

namespace ShopQuanAo.Areas.Identity.Pages.Account
{
	public class LoginModel : PageModel
	{
		// SỬA: Thay ApplicationUser bằng ApplicationUser
		private readonly SignInManager<ApplicationUser> _signInManager;
		private readonly ILogger<LoginModel> _logger;
		private readonly UserManager<ApplicationUser> _userManager;

		public LoginModel(SignInManager<ApplicationUser> signInManager,
			ILogger<LoginModel> logger,
			UserManager<ApplicationUser> userManager)
		{
			_signInManager = signInManager;
			_logger = logger;
			_userManager = userManager;
		}

		[BindProperty]
		public InputModel Input { get; set; }

		public IList<AuthenticationScheme> ExternalLogins { get; set; }

		public string ReturnUrl { get; set; }

		[TempData]
		public string ErrorMessage { get; set; }

		public class InputModel
		{
			[Required(ErrorMessage = "Vui lòng nhập Email")]
			[EmailAddress(ErrorMessage = "Email không đúng định dạng")]
			public string Email { get; set; }

			[Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
			[DataType(DataType.Password)]
			public string Password { get; set; }

			[Display(Name = "Ghi nhớ đăng nhập?")]
			public bool RememberMe { get; set; }
		}

		public async Task OnGetAsync(string returnUrl = null)
		{
			if (!string.IsNullOrEmpty(ErrorMessage))
			{
				ModelState.AddModelError(string.Empty, ErrorMessage);
			}

			returnUrl ??= Url.Content("~/");

			await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

			ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

			ReturnUrl = returnUrl;
		}

		public async Task<IActionResult> OnPostAsync(string returnUrl = null)
		{
			returnUrl ??= Url.Content("~/");

			ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

			if (ModelState.IsValid)
			{
				// Thực hiện đăng nhập
				var result = await _signInManager.PasswordSignInAsync(Input.Email, Input.Password, Input.RememberMe, lockoutOnFailure: false);

				if (result.Succeeded)
				{
					_logger.LogInformation("User logged in.");
					return LocalRedirect(returnUrl);
				}

				// Trường hợp tài khoản chưa được xác nhận (chưa nhập OTP)
				if (result.IsNotAllowed)
				{
					// Chuyển hướng người dùng đến trang nhập OTP nếu họ chưa xác thực
					ModelState.AddModelError(string.Empty, "Tài khoản của bạn chưa được xác thực mã OTP. Vui lòng kiểm tra email.");
					return Page();
				}

				if (result.RequiresTwoFactor)
				{
					return RedirectToPage("./LoginWith2fa", new { ReturnUrl = returnUrl, RememberMe = Input.RememberMe });
				}

				if (result.IsLockedOut)
				{
					_logger.LogWarning("Tài khoản bị khóa.");
					return RedirectToPage("./Lockout");
				}
				else
				{
					ModelState.AddModelError(string.Empty, "Email hoặc mật khẩu không chính xác.");
					return Page();
				}
			}

			return Page();
		}
	}
}