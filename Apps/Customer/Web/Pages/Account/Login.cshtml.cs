using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using TrangSach.Customer.BUS;

namespace TrangSach.Customer.Web.Pages.Account;

public sealed class LoginModel(CustomerWebApiClient api) : PageModel
{
    [BindProperty] public InputModel Input { get; set; } = new();
    [BindProperty(SupportsGet = true)] public string? ReturnUrl { get; set; }

    public void OnGet() => ViewData["Title"] = "Đăng nhập";

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        ViewData["Title"] = "Đăng nhập";
        if (!ModelState.IsValid) return Page();
        try
        {
            var auth = await api.LoginAsync(Input.UserNameOrEmail.Trim(), Input.Password, ct)
                ?? throw new InvalidOperationException("Máy chủ không trả về thông tin đăng nhập.");
            if (auth.Role != "Customer") throw new InvalidOperationException("Tài khoản nhân viên hãy đăng nhập trong khu vực Quản lý.");
            await SignInAsync(auth);
            return !string.IsNullOrWhiteSpace(ReturnUrl) && Url.IsLocalUrl(ReturnUrl)
                ? LocalRedirect(ReturnUrl)
                : RedirectToPage("/Index");
        }
        catch (Exception ex) { ModelState.AddModelError(string.Empty, ex.Message); return Page(); }
    }

    private async Task SignInAsync(TrangSach.Shared.DTO.StoreAuthResultDto auth)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, auth.CustomerId ?? auth.UserName),
            new(ClaimTypes.Name, auth.UserName),
            new(ClaimTypes.Role, auth.Role),
            new("access_token", auth.AccessToken)
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var expires = auth.ExpiresAtUtc > DateTime.UtcNow ? auth.ExpiresAtUtc : DateTime.UtcNow.AddMinutes(90);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity), new AuthenticationProperties { IsPersistent = Input.RememberMe, ExpiresUtc = expires });
    }

    public sealed class InputModel
    {
        [Required, Display(Name = "Tên đăng nhập hoặc email")] public string UserNameOrEmail { get; set; } = string.Empty;
        [Required, DataType(DataType.Password), Display(Name = "Mật khẩu")] public string Password { get; set; } = string.Empty;
        [Display(Name = "Ghi nhớ đăng nhập")] public bool RememberMe { get; set; }
    }
}
