using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using TrangSach.Shared.DTO;
using TrangSach.Shared.Security;

namespace TrangSach.Management.Web.GUI.Pages;

public abstract class AdminPageModel : PageModel
{
    protected string Token => User.FindFirstValue("access_token") ?? throw new InvalidOperationException("Phiên đăng nhập hết hạn. Vui lòng đăng nhập lại.");
}

public abstract class AdminAccountPageModel : PageModel
{
    protected async Task SignInStaffAsync(StoreAuthResultDto account, bool rememberMe = false)
    {
        if (!StaffPermissions.IsEmployeeRole(account.Role)) throw new InvalidOperationException("Đây không phải tài khoản nhân viên hợp lệ.");
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, account.UserName),
            new(ClaimTypes.Name, account.UserName),
            new(ClaimTypes.Role, account.Role),
            new("access_token", account.AccessToken)
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var expires = account.ExpiresAtUtc > DateTime.UtcNow ? account.ExpiresAtUtc : DateTime.UtcNow.AddMinutes(90);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity),
            new AuthenticationProperties { IsPersistent = rememberMe, ExpiresUtc = expires });
    }
}
