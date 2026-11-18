using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using TrangSach.Customer.BUS;

namespace TrangSach.Customer.Web.Pages.Account;

public sealed class RegisterModel(CustomerWebApiClient api) : PageModel
{
    [BindProperty] public InputModel Input { get; set; } = new();

    public void OnGet() => ViewData["Title"] = "Tạo tài khoản";

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        ViewData["Title"] = "Tạo tài khoản";
        if (!ModelState.IsValid) return Page();
        try
        {
            var auth = await api.RegisterAsync(Input.UserName.Trim(), Input.Email.Trim(), Input.Password,
                Input.FullName.Trim(), string.IsNullOrWhiteSpace(Input.PhoneNumber) ? null : Input.PhoneNumber.Trim(), ct)
                ?? throw new InvalidOperationException("Máy chủ không trả về thông tin tài khoản.");
            if (auth.Role != "Customer") throw new InvalidOperationException("Chỉ tài khoản khách hàng được đăng ký tại đây.");
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, auth.CustomerId ?? auth.UserName),
                new(ClaimTypes.Name, auth.UserName),
                new(ClaimTypes.Role, auth.Role),
                new("access_token", auth.AccessToken)
            };
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)),
                new AuthenticationProperties { IsPersistent = true, ExpiresUtc = auth.ExpiresAtUtc });
            TempData["Notice"] = "Tạo tài khoản thành công. Chào mừng bạn đến với Trang Sách!";
            return RedirectToPage("/Index");
        }
        catch (Exception ex) { ModelState.AddModelError(string.Empty, ex.Message); return Page(); }
    }

    public sealed class InputModel
    {
        [Required, StringLength(80, MinimumLength = 3), Display(Name = "Tên đăng nhập")] public string UserName { get; set; } = string.Empty;
        [Required, EmailAddress, StringLength(254), Display(Name = "Email")] public string Email { get; set; } = string.Empty;
        [Required, StringLength(100, MinimumLength = 8), DataType(DataType.Password), Display(Name = "Mật khẩu (ít nhất 8 ký tự)")] public string Password { get; set; } = string.Empty;
        [Required, StringLength(180, MinimumLength = 2), Display(Name = "Họ và tên")] public string FullName { get; set; } = string.Empty;
        [Phone, StringLength(30), Display(Name = "Số điện thoại (không bắt buộc)")] public string? PhoneNumber { get; set; }
    }
}
