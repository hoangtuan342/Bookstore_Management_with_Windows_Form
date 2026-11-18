using Microsoft.AspNetCore.Authorization;
using TrangSach.Management.Web.BUS;
using TrangSach.Shared.DTO;
using TrangSach.Shared.Security;

namespace TrangSach.Management.Web.GUI.Pages.Admin;

[Authorize(Roles = StaffRoles.ManagerOrAccountant)]
public sealed class IndexModel(ManagementWebApiClient api) : AdminPageModel
{
    public ManagerDashboardDto? Summary { get; private set; }
    public ManagerReportDto? WeeklyTrend { get; private set; }
    public async Task OnGetAsync(CancellationToken ct)
    {
        var dashboardTask = api.GetDashboardAsync(Token, ct);
        var trendTask = api.GetReportAsync(Token, 7, ct);
        await Task.WhenAll(dashboardTask, trendTask);
        Summary = await dashboardTask;
        WeeklyTrend = await trendTask;
        ViewData["Title"] = "Tổng quan";
    }
    public int BarHeight(decimal value) => WeeklyTrend?.RevenueByDayDto.Count is > 0
        ? Math.Max(5, (int)Math.Round(value * 100m / Math.Max(1m, WeeklyTrend.RevenueByDayDto.Max(row => row.Revenue))))
        : 5;
}
