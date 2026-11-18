using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrangSach.Management.Web.BUS;
using TrangSach.Shared.DTO;
using TrangSach.Shared.Security;

namespace TrangSach.Management.Web.GUI.Pages.Admin.Reports;

[Authorize(Roles = StaffRoles.ManagerOrAccountant)]
public sealed class DailyModel(ManagementWebApiClient api) : AdminPageModel
{
    [BindProperty(SupportsGet = true)] public DateOnly? FromDate { get; set; }
    [BindProperty(SupportsGet = true)] public DateOnly? ToDate { get; set; }
    [BindProperty(SupportsGet = true)] public int Days { get; set; } = 30;
    public ManagerReportDto? Report { get; private set; }
    public ReportChartPanel? Chart { get; private set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken ct)
    {
        if (FromDate.HasValue && ToDate.HasValue)
        {
            if (ToDate < FromDate) return BadRequest("Ngày kết thúc phải bằng hoặc sau ngày bắt đầu.");
            Report = await api.GetReportAsync(Token, FromDate.Value, ToDate.Value, ct);
        }
        else
        {
            Days = Math.Clamp(Days, 1, 3650);
            Report = await api.GetReportAsync(Token, Days, ct);
        }
        if (Report is null) return NotFound();
        FromDate = DateOnly.FromDateTime(Report.FromDate);
        ToDate = DateOnly.FromDateTime(Report.ToDate);
        var rows = Report.RevenueByDayDto.OrderBy(x => x.Date).ToArray();
        static ReportChartItem Point(RevenueByDayDto row, decimal value) => new(row.Date.ToString("dd/MM/yyyy"), value, value.ToString("N0") + " ₫");
        Chart = new ReportChartPanel("Doanh thu và nhập hàng theo ngày",
            rows.Select(row => Point(row, row.Revenue)).ToList(), ReportChartType.Line,
            [new ReportChartSeries("Doanh thu", "#456f55", rows.Select(row => Point(row, row.Revenue)).ToList()),
             new ReportChartSeries("Nhập hàng", "#d4a268", rows.Select(row => Point(row, row.PurchaseSpend)).ToList())]);
        ViewData["Title"] = "Doanh thu theo ngày";
        return Page();
    }
}
