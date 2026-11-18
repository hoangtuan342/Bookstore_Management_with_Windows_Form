using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrangSach.Management.Web.BUS;
using TrangSach.Shared.DTO;
using TrangSach.Shared.Security;

namespace TrangSach.Management.Web.GUI.Pages.Admin.Profile;

[Authorize(Roles = "Manager,Sales,Inventory,Catalog,Marketing,Accountant")]
public sealed class IndexModel(ManagementWebApiClient api) : AdminPageModel
{
    public AccountProfileDto? Profile { get; private set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken ct)
    {
        var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        if (!StaffPermissions.CanAccess(role, "profile")) return Forbid();
        Profile = await api.GetProfileAsync(Token, ct);
        if (Profile is null) return Unauthorized();
        ViewData["Title"] = "Hồ sơ cá nhân";
        return Page();
    }
}
