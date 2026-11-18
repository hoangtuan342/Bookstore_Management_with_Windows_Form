using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrangSach.Management.Web.BUS;
using TrangSach.Shared.DTO;
using TrangSach.Shared.Security;

namespace TrangSach.Management.Web.GUI.Pages.Admin.Vouchers;

[Authorize(Roles = StaffRoles.ManagerOrMarketing)]
public sealed class IndexModel(ManagementWebApiClient api) : AdminPageModel
{
    public IReadOnlyList<VoucherSummaryDto> Vouchers { get; private set; } = [];
    public async Task OnGetAsync(CancellationToken ct) { Vouchers = await api.GetVouchersAsync(Token, ct); ViewData["Title"] = "Voucher"; }
    public async Task<IActionResult> OnPostUpdateAsync(string id, string status, string? expiresAt, string? maxUsageCount, CancellationToken ct)
    {
        try
        {
            DateTime? expires = DateTime.TryParse(expiresAt, CultureInfo.InvariantCulture, out var date) ? date : null;
            int? max = int.TryParse(maxUsageCount, out var value) ? value : null;
            await api.UpdateVoucherAsync(Token, id, new VoucherUpdateCommandDto(status, expires, max), ct);
            TempData["Notice"] = "Đã cập nhật voucher.";
        }
        catch (Exception ex) { TempData["Notice"] = ex.Message; }
        return RedirectToPage();
    }
    public async Task<IActionResult> OnPostDeleteAsync(string id, CancellationToken ct)
    {
        try { await api.DeleteVoucherAsync(Token, id, ct); TempData["Notice"] = "Đã xóa voucher."; }
        catch (Exception ex) { TempData["Notice"] = ex.Message; }
        return RedirectToPage();
    }
}
