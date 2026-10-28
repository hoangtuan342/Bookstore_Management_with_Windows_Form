using BookStore.Shared.Models;
using BookStore.Shared.Security;

namespace TrangSach.Management.Forms;

public partial class ManagementForm
{
    private async Task LoadDashboardAsync()
    {
        var value = await _services.Manager.GetDashboardAsync();
        _dashboardGrid.DataSource = value.RecentOrders.ToList();
        _status.Text = $"Doanh thu tháng: {MoneyFormatter.ToVnd(value.RevenueThisMonth)} · Đơn hôm nay: {value.OrdersToday} · Chờ xử lý: {value.PendingOrders} · Sách sắp hết: {value.LowStockBooks}";
    }

    private async Task LoadBooksAsync() => _booksGrid.DataSource = (await _services.Manager.GetBooksAsync()).ToList();

    private async Task LoadCustomersAsync() => _customersGrid.DataSource = (await _services.Manager.GetCustomersAsync()).ToList();

    private async Task LoadEmployeesAsync() => _employeesGrid.DataSource = (await _services.Manager.GetEmployeesAsync()).ToList();

    private async Task LoadOrdersAsync() => _ordersGrid.DataSource = (await _services.Manager.GetOrdersAsync()).ToList();

    private async Task LoadReceiptsAsync() => _receiptsGrid.DataSource = (await _services.Manager.GetReceiptsAsync()).ToList();

    private async Task LoadSuppliersAsync() => _suppliersGrid.DataSource = (await _services.Manager.GetLookupsAsync()).Suppliers.ToList();

    private async Task LoadPromotionsAsync() => _promotionsGrid.DataSource = (await _services.Manager.GetPromotionsAsync()).ToList();

    private async Task LoadVouchersAsync() => _vouchersGrid.DataSource = (await _services.Manager.GetVouchersAsync()).ToList();

    private async Task LoadLogsAsync() => _logsGrid.DataSource = (await _services.Manager.GetAuditLogsAsync()).ToList();

    private async Task LoadReportAsync(int days)
    {
        if (_reportView is null) return;
        _reportView.SetLoading(days);
        var report = await _services.Manager.GetReportAsync(days);
        _reportView.ShowReport(report);
    }
}

