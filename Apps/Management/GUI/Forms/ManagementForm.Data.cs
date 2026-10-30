using BookStore.Shared.Models;
using BookStore.Shared.Security;
using TrangSach.BUS.Models;

namespace TrangSach.Management.Forms;

public partial class ManagementForm
{
    private async Task LoadDashboardAsync(bool refresh = false)
    {
        var value = await _services.ReadCachedAsync("dashboard", () => _services.Manager.GetDashboardAsync(), refresh);
        _dashboardGrid.DataSource = value.RecentOrders.ToList();
        _status.Text = $"Doanh thu tháng: {MoneyFormatter.ToVnd(value.RevenueThisMonth)} · Đơn hôm nay: {value.OrdersToday} · Chờ xử lý: {value.PendingOrders} · Sách sắp hết: {value.LowStockBooks}";
    }

    private async Task LoadBooksAsync(bool refresh = false) => _booksGrid.DataSource = (await _services.ReadCachedAsync("books", () => _services.Manager.GetBooksAsync(), refresh)).ToList();

    private async Task LoadCustomersAsync(bool refresh = false) => _customersGrid.DataSource = (await _services.ReadCachedAsync("customers", () => _services.Manager.GetCustomersAsync(), refresh)).ToList();

    private async Task LoadEmployeesAsync(bool refresh = false) => _employeesGrid.DataSource = (await _services.ReadCachedAsync("employees", () => _services.Manager.GetEmployeesAsync(), refresh)).ToList();

    private async Task LoadOrdersAsync(bool refresh = false) => _ordersGrid.DataSource = (await _services.ReadCachedAsync("orders", () => _services.Manager.GetOrdersAsync(), refresh)).ToList();

    private async Task LoadReceiptsAsync(bool refresh = false) => _receiptsGrid.DataSource = (await _services.ReadCachedAsync("receipts", () => _services.Manager.GetReceiptsAsync(), refresh)).ToList();

    private async Task LoadSuppliersAsync(bool refresh = false) => _suppliersGrid.DataSource = (await _services.ReadCachedAsync("lookups", () => _services.Manager.GetLookupsAsync(), refresh)).Suppliers.ToList();

    private Task<ManagerLookups> GetLookupsAsync(bool refresh = false) =>
        _services.ReadCachedAsync("lookups", () => _services.Manager.GetLookupsAsync(), refresh);

    private async Task LoadPromotionsAsync(bool refresh = false) => _promotionsGrid.DataSource = (await _services.ReadCachedAsync("promotions", () => _services.Manager.GetPromotionsAsync(), refresh)).ToList();

    private async Task LoadVouchersAsync(bool refresh = false) => _vouchersGrid.DataSource = (await _services.ReadCachedAsync("vouchers", () => _services.Manager.GetVouchersAsync(), refresh)).ToList();

    private async Task LoadLogsAsync(bool refresh = false) => _logsGrid.DataSource = (await _services.ReadCachedAsync("logs", () => _services.Manager.GetAuditLogsAsync(), refresh)).ToList();

    private async Task LoadReportAsync(int days)
    {
        if (_reportView is null) return;
        _reportView.SetLoading(days);
        var report = await _services.Manager.GetReportAsync(days);
        _reportView.ShowReport(report);
    }

    private async Task LoadReportAsync(DateOnly fromDate, DateOnly toDate)
    {
        if (_reportView is null) return;
        if (toDate < fromDate) throw new InvalidOperationException("Ngày kết thúc phải bằng hoặc sau ngày bắt đầu.");
        _reportView.SetLoading(fromDate, toDate);
        var report = await _services.Manager.GetReportAsync(fromDate, toDate);
        _reportView.ShowReport(report);
    }

    private void ExportReportPdf(ManagerReport report)
    {
        using var dialog = new SaveFileDialog { Filter = "PDF (*.pdf)|*.pdf", FileName = $"bao-cao-{report.FromDate:yyyyMMdd}-{report.ToDate:yyyyMMdd}.pdf" };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        _reportFiles.ExportPdf(report, dialog.FileName);
        MessageBox.Show(this, "Đã xuất báo cáo PDF.", "Xuất báo cáo", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void ExportReportExcel(ManagerReport report)
    {
        using var dialog = new SaveFileDialog { Filter = "Excel Workbook (*.xlsx)|*.xlsx|Excel 97-2003 (*.xls)|*.xls", FileName = $"bao-cao-{report.FromDate:yyyyMMdd}-{report.ToDate:yyyyMMdd}.xlsx" };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        _reportFiles.ExportExcel(report, dialog.FileName);
        MessageBox.Show(this, "Đã xuất báo cáo Excel với các trang tổng quan, theo tháng, nhân viên và sản phẩm.", "Xuất báo cáo", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
}

