using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using TrangSach.Customer.BUS;

namespace TrangSach.Customer.Web.Pages;

public abstract class CustomerPageModel : PageModel
{
    private const string GuestCookieName = "TrangSach.GuestId";

    protected string? ApiToken => User.FindFirstValue("access_token");

    protected string RequireApiToken() => ApiToken
        ?? throw new InvalidOperationException("Phiên làm việc đã hết hạn. Vui lòng thử lại.");

    /// <summary>
    /// Đảm bảo luôn có token (nếu là khách vãng lai chưa đăng nhập, tự động cấp JWT Guest dựa theo cookie).
    /// </summary>
    protected async Task<string> EnsureCustomerTokenAsync(CustomerWebApiClient api, CancellationToken ct = default)
    {
        if (!string.IsNullOrWhiteSpace(ApiToken))
        {
            return ApiToken;
        }

        // Kiểm tra cookie khách vãng lai hoặc sinh mã mới
        var guestId = Request.Cookies[GuestCookieName];
        if (string.IsNullOrWhiteSpace(guestId) || !guestId.StartsWith("KH-GUEST-"))
        {
            guestId = $"KH-GUEST-{Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(4))}";
            Response.Cookies.Append(GuestCookieName, guestId, new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddDays(30),
                HttpOnly = true,
                Secure = Request.IsHttps,
                SameSite = SameSiteMode.Lax
            });
        }

        var auth = await api.GetGuestTokenAsync(guestId, ct)
            ?? throw new InvalidOperationException("Không thể khởi tạo phiên khách vãng lai.");

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, auth.CustomerId ?? guestId),
            new(ClaimTypes.Name, "Khách vãng lai"),
            new(ClaimTypes.Role, "Customer"),
            new("customer_id", auth.CustomerId ?? guestId),
            new("is_guest", "true"),
            new("access_token", auth.AccessToken)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var expires = auth.ExpiresAtUtc > DateTime.UtcNow ? auth.ExpiresAtUtc : DateTime.UtcNow.AddMinutes(90);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties { IsPersistent = true, ExpiresUtc = expires });

        return auth.AccessToken;
    }

    protected void AddApiError(Exception exception) => ModelState.AddModelError(string.Empty, exception.Message);
}
