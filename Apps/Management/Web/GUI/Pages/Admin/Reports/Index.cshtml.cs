using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrangSach.Management.Web.BUS;
using TrangSach.Shared.DTO;
using TrangSach.Shared.Security;

namespace TrangSach.Management.Web.GUI.Pages.Admin.Reports;

[Authorize(Roles = StaffRoles.ManagerOrAccountant)]
public sealed class IndexModel(ManagementWebApiClient api, ManagementSpreadsheetService exports) : AdminPageModel
{
    public ManagerReportDto? Report { get; private set; }
    public ManagerReportDto? HistoryReport { get; private set; }
    public List<ReportChartPanel> ChartPanels { get; private set; } = [];
    [BindProperty(SupportsGet = true)] public int Days { get; set; } = 1825;
    [BindProperty(SupportsGet = true)] public DateOnly? FromDate { get; set; }
    [BindProperty(SupportsGet = true)] public DateOnly? ToDate { get; set; }
    public async Task OnGetAsync(CancellationToken ct)
    {
        Days = Math.Clamp(Days, 1, 3650);
        Report = await LoadReportAsync(ct);
        if (Report is not null)
        {
            Days = Report.Days;
            FromDate ??= DateOnly.FromDateTime(Report.FromDate);
            ToDate ??= DateOnly.FromDateTime(Report.ToDate);
            HistoryReport = Report.Days >= 1820 ? Report : await api.GetReportAsync(Token, 1825, ct);
            ChartPanels = BuildCharts(Report, HistoryReport ?? Report);
        }
        ViewData["Title"] = "Báo cáo";
    }
    public async Task<IActionResult> OnGetExportAsync(CancellationToken ct)
    {
        var report = await LoadReportAsync(ct);
        if (report is null) return NotFound();
        var csv = new StringBuilder("Date;Orders;Revenue;PurchaseSpend\r\n");
        foreach (var row in report.RevenueByDayDto) csv.AppendLine(string.Join(';', row.Date.ToString("yyyy-MM-dd"), row.Orders, row.Revenue.ToString(CultureInfo.InvariantCulture), row.PurchaseSpend.ToString(CultureInfo.InvariantCulture)));
        return File(new UTF8Encoding(true).GetBytes(csv.ToString()), "text/csv; charset=utf-8", $"bao-cao-{report.Days}-ngay.csv");
    }

    public async Task<IActionResult> OnGetExportExcelAsync(CancellationToken ct)
    {
        var report = await LoadReportAsync(ct);
        if (report is null) return NotFound();
        var bytes = exports.CreateReportWorkbook(report);
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"bao-cao-{report.FromDate:yyyyMMdd}-{report.ToDate:yyyyMMdd}.xlsx");
    }

    public async Task<IActionResult> OnGetExportPdfAsync(CancellationToken ct)
    {
        var report = await LoadReportAsync(ct);
        if (report is null) return NotFound();
        var bytes = exports.CreateReportPdf(report);
        return File(bytes, "application/pdf", $"bao-cao-{report.FromDate:yyyyMMdd}-{report.ToDate:yyyyMMdd}.pdf");
    }

    public int BarHeight(decimal value) => Report?.RevenueByDayDto.Count is > 0
        ? Math.Max(5, (int)Math.Round(value * 100m / Math.Max(1m, Report.RevenueByDayDto.Max(row => row.Revenue))))
        : 5;

    private static List<ReportChartPanel> BuildCharts(ManagerReportDto report, ManagerReportDto history)
    {
        static ReportChartItem Money(string label, decimal value) => new(label, value, value.ToString("N0") + " ₫");
        static List<ReportChartSeries> RevenueAndPurchases<T>(IEnumerable<T> rows, Func<T, string> label, Func<T, decimal> revenue, Func<T, decimal> purchases) =>
        [
            new("Doanh thu", "#456f55", rows.Select(x => Money(label(x), revenue(x))).ToList()),
            new("Nhập hàng", "#d4a268", rows.Select(x => Money(label(x), purchases(x))).ToList())
        ];

        var daily = report.RevenueByDayDto.OrderBy(x => x.Date).TakeLast(14).ToList();
        var charts = new List<ReportChartPanel>
        {
            new("Doanh thu theo ngày · 14 ngày gần nhất", daily.Select(x => Money(x.Date.ToString("dd/MM"), x.Revenue)).ToList(), ReportChartType.Line,
                [new("Doanh thu", "#456f55", daily.Select(x => Money(x.Date.ToString("dd/MM"), x.Revenue)).ToList()),
                 new("Nhập hàng", "#d4a268", daily.Select(x => Money(x.Date.ToString("dd/MM"), x.PurchaseSpend)).ToList())], true, DateOnly.FromDateTime(report.FromDate), DateOnly.FromDateTime(report.ToDate)),
            new("Doanh thu theo tháng · 5 năm", history.RevenueByMonth.TakeLast(24).Select(x => Money($"{x.Month:00}/{x.Year}", x.Revenue)).ToList(), ReportChartType.Line,
                [.. RevenueAndPurchases(history.RevenueByMonth.TakeLast(24), x => $"{x.Month:00}/{x.Year}", x => x.Revenue, x => x.PurchaseSpend)]),
            new("Doanh thu theo quý · 5 năm", history.RevenueByQuarter.Select(x => Money($"Q{x.Quarter}/{x.Year}", x.Revenue)).ToList(), ReportChartType.Line,
                [.. RevenueAndPurchases(history.RevenueByQuarter, x => $"Q{x.Quarter}/{x.Year}", x => x.Revenue, x => x.PurchaseSpend)]),
            new("Doanh thu theo năm · 5 năm", history.RevenueByYear.Where(x => x.Revenue > 0 || x.PurchaseSpend > 0).Select(x => Money(x.Year.ToString(), x.Revenue)).ToList(), ReportChartType.Column),
            new("Doanh thu theo nhân viên / năm · 5 năm", history.RevenueByEmployeeYear.OrderByDescending(x => x.Revenue).Take(8).Select(x => Money($"{x.EmployeeName} · {x.Year}", x.Revenue)).ToList(), ReportChartType.HorizontalBar),
            new("Sản phẩm bán theo quý / năm · 5 năm", history.ProductSalesByQuarter.OrderByDescending(x => x.Revenue).Take(8).Select(x => Money($"{x.Title} · Q{x.Quarter}/{x.Year}", x.Revenue)).ToList(), ReportChartType.HorizontalBar),
            new("Tồn kho theo thể loại", report.InventoryByCategory.Select(x => new ReportChartItem(x.Category, x.StockQuantity, $"{x.StockQuantity:N0} cuốn tồn · {x.LowStockBooks:N0} sắp hết")).ToList(), ReportChartType.HorizontalBar),
            new("Sách bán chạy", report.BestSellers.OrderByDescending(x => x.Quantity).Take(8).Select(x => new ReportChartItem(x.Title, x.Quantity, $"{x.Quantity:N0} cuốn")).ToList(), ReportChartType.HorizontalBar),
            new("Tình trạng đơn hàng", new List<ReportChartItem>
            {
                new ReportChartItem("Hoàn tất", report.CompletedOrders, $"{report.CompletedOrders:N0} đơn"),
                new ReportChartItem("Đang xử lý", report.ProcessingOrders, $"{report.ProcessingOrders:N0} đơn"),
                new ReportChartItem("Chờ xác nhận", report.PendingOrders, $"{report.PendingOrders:N0} đơn"),
                new ReportChartItem("Đã hủy", report.CancelledOrders, $"{report.CancelledOrders:N0} đơn")
            }, ReportChartType.Donut),
            new("Tỷ trọng doanh thu theo thể loại", report.RevenueByCategory.OrderByDescending(x => x.Revenue).Select(x => Money(x.Category, x.Revenue)).ToList(), ReportChartType.Donut),
            new("Khách hàng mua nhiều", report.TopCustomers.OrderByDescending(x => x.Revenue).Take(8).Select(x => Money(x.CustomerName, x.Revenue)).ToList(), ReportChartType.HorizontalBar)
        };
        return charts;
    }

    private Task<ManagerReportDto?> LoadReportAsync(CancellationToken ct) => FromDate.HasValue && ToDate.HasValue
        ? api.GetReportAsync(Token, FromDate.Value, ToDate.Value, ct)
        : api.GetReportAsync(Token, Math.Clamp(Days, 1, 3650), ct);
}

public enum ReportChartType { Line, Column, HorizontalBar, Donut }

public sealed record ReportChartItem(string Label, decimal Value, string Display);

public sealed record ReportChartSeries(string Name, string Color, List<ReportChartItem> Items);

public sealed record ReportPlotPoint(double X, double Y, string Label, string Display);

public sealed record ReportChartPanel(string Title, List<ReportChartItem> Items, ReportChartType Type,
    List<ReportChartSeries>? Series = null, bool IsDaily = false, DateOnly? FromDate = null, DateOnly? ToDate = null)
{
    public decimal Maximum => Items.Select(item => item.Value).DefaultIfEmpty(0).Max();
    public int Height(decimal value) => Maximum <= 0 ? 0 : value <= 0 ? 2 : Math.Clamp((int)Math.Round(value * 100m / Maximum), 3, 100);
    public List<ReportChartItem> AxisLabels
    {
        get
        {
            var labels = Series?.FirstOrDefault()?.Items ?? Items;
            if (labels.Count <= 8) return labels;
            var step = (int)Math.Ceiling(labels.Count / 7d);
            return labels.Where((_, index) => index % step == 0 || index == labels.Count - 1).ToList();
        }
    }
    public List<ReportPlotPoint> PlotPoints(ReportChartSeries series)
    {
        var values = Series?.SelectMany(x => x.Items).Select(x => x.Value).ToArray() ?? series.Items.Select(x => x.Value).ToArray();
        var max = values.DefaultIfEmpty(0).Max();
        const double width = 580, height = 125, left = 10, top = 8;
        return series.Items.Select((item, index) => new ReportPlotPoint(
            series.Items.Count <= 1 ? width / 2 : left + index * (width - 2 * left) / (series.Items.Count - 1),
            max <= 0 ? height + top : top + height - (double)item.Value / (double)max * height,
            item.Label, item.Display)).ToList();
    }
    public string Coordinate(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);
    public string PointsAttribute(List<ReportPlotPoint> points) =>
        string.Join(" ", points.Select(point => $"{Coordinate(point.X)},{Coordinate(point.Y)}"));
    public string DonutGradient
    {
        get
        {
            var total = Items.Sum(x => Math.Max(0, x.Value));
            if (total <= 0) return "conic-gradient(#e8edf0 0% 100%)";
            string[] colors = ["#456f55", "#d4a268", "#517d99", "#bd6d5a", "#8d79a8", "#72a6a0", "#a6ad5c", "#77828b"];
            var start = 0m;
            var stops = new List<string>();
            for (var i = 0; i < Items.Count; i++)
            {
                var end = start + Math.Max(0, Items[i].Value) * 100m / total;
                stops.Add($"{colors[i % colors.Length]} {start.ToString("0.##", CultureInfo.InvariantCulture)}% {end.ToString("0.##", CultureInfo.InvariantCulture)}%");
                start = end;
            }
            return $"conic-gradient({string.Join(",", stops)})";
        }
    }
}
