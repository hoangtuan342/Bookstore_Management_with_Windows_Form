using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace TrangSach.Customer.Web.Pages;

public abstract class CustomerPageModel : PageModel
{
    protected string? ApiToken => User.FindFirstValue("access_token");

    protected string RequireApiToken() => ApiToken
        ?? throw new InvalidOperationException("Phiên đăng nhập hết hạn. Vui lòng đăng nhập lại.");

    protected void AddApiError(Exception exception) => ModelState.AddModelError(string.Empty, exception.Message);
}
