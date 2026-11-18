using System.Security.Claims;
using TrangSach.Shared.Security;

namespace TrangSach.Management.Web.GUI.Pages;

public sealed class IndexModel : PageModel
{
    public IActionResult OnGet() => User.Identity?.IsAuthenticated == true
        ? LocalRedirect(StaffPermissions.HomePath(User.FindFirstValue(ClaimTypes.Role)))
        : RedirectToPage("/Account/Login");
}
