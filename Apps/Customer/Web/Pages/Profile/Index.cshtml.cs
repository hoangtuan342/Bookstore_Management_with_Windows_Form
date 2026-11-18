using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrangSach.Customer.BUS;
using TrangSach.Shared.DTO;

namespace TrangSach.Customer.Web.Pages.Profile;

[Authorize(Roles = "Customer")]
public sealed class IndexModel(CustomerWebApiClient api) : CustomerPageModel
{
    public AccountProfileDto? Profile { get; private set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken ct)
    {
        Profile = await api.GetProfileAsync(RequireApiToken(), ct);
        if (Profile is null) return Unauthorized();
        ViewData["Title"] = "Hồ sơ cá nhân";
        return Page();
    }
}
