using Microsoft.AspNetCore.Authorization;
using TrangSach.Management.Web.BUS;
using TrangSach.Shared.DTO;
using TrangSach.Shared.Security;

namespace TrangSach.Management.Web.GUI.Pages.Admin.Audit;

[Authorize(Roles = StaffRoles.Manager)]
public sealed class IndexModel(ManagementWebApiClient api) : AdminPageModel
{
    public IReadOnlyList<ManagerAuditLogDto> Logs { get; private set; } = [];
    public async Task OnGetAsync(CancellationToken ct) { Logs = await api.GetAuditLogsAsync(Token, ct); ViewData["Title"] = "Nhật ký hệ thống"; }
}
