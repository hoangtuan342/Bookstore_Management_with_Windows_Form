using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using System.Text.Json;
using TrangSach.Customer.BUS;

namespace TrangSach.Customer.Web.Pages;

public abstract class CustomerPageModel : PageModel
{
    private const string GuestCookieName = "TrangSach.GuestId";

    protected string? ApiToken => User.FindFirstValue("access_token");

    protected string RequireApiToken() => ApiToken
        ?? throw new InvalidOperationException("Phiên làm việc đã hết hạn. Vui lòng thử lại.");

    /// <summary>
    /// Đảm bảo luôn có token hợp lệ. Nếu token đã hết hạn hoặc là khách vãng lai, tự động cấp hoặc làm mới JWT Guest.
    /// </summary>
    protected async Task<string> EnsureCustomerTokenAsync(CustomerWebApiClient api, CancellationToken ct = default, bool forceRefresh = false)
    {
        if (!forceRefresh && !string.IsNullOrWhiteSpace(ApiToken))
        {
            if (IsJwtTokenValid(ApiToken))
            {
                return ApiToken;
            }
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

    private static bool IsJwtTokenValid(string token)
    {
        try
        {
            var parts = token.Split('.');
            if (parts.Length < 2) return false;
            var payload = parts[1];
            payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
            var bytes = Convert.FromBase64String(payload.Replace('-', '+').Replace('_', '/'));
            using var doc = JsonDocument.Parse(bytes);
            if (doc.RootElement.TryGetProperty("exp", out var expElement) && expElement.TryGetInt64(out var exp))
            {
                var expTime = DateTimeOffset.FromUnixTimeSeconds(exp).UtcDateTime;
                return expTime > DateTime.UtcNow.AddMinutes(1);
            }
            return true;
        }
        catch
        {
            return false;
        }
    }

    protected void AddApiError(Exception exception) => ModelState.AddModelError(string.Empty, exception.Message);
}
