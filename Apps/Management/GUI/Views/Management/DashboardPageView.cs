namespace TrangSach.Management.Views;

internal sealed class DashboardPageView : ManagementGridPageView
{
    private readonly ComboBox _status = ManagementSearchFilterControls.Choice(150,
        new(null, "Mọi trạng thái"), new("Pending", "Chờ xác nhận"), new("Confirmed", "Đã xác nhận"),
        new("Shipping", "Đang giao"), new("Completed", "Hoàn tất"), new("Cancelled", "Đã hủy"));
    private readonly DateTimePicker _from = ManagementSearchFilterControls.Date();
    private readonly DateTimePicker _to = ManagementSearchFilterControls.Date();

    public DashboardPageView() : base("Tổng quan")
    {
        _status.SelectedValueChanged += (_, _) => RefreshFilter();
        _from.ValueChanged += (_, _) => RefreshFilter();
        _to.ValueChanged += (_, _) => RefreshFilter();
        AddSearchFilterControl(ManagementSearchFilterControls.Label("Trạng thái đơn"));
        AddSearchFilterControl(_status);
        AddSearchFilterControl(ManagementSearchFilterControls.Label("Từ"));
        AddSearchFilterControl(_from);
        AddSearchFilterControl(ManagementSearchFilterControls.Label("Đến"));
        AddSearchFilterControl(_to);
        AddAction("Làm mới", "refresh");
    }

    protected override bool HasAdditionalFilters => ManagementSearchFilterControls.Value(_status) is not null || _from.Checked || _to.Checked;

    protected override bool MatchesAdditionalFilters(object item)
    {
        if (item is not BookStore.Shared.Models.ManagerRecentOrder order) return true;
        var status = ManagementSearchFilterControls.Value(_status);
        var date = order.CreatedAt.ToLocalTime().Date;
        return (status is null || order.OrderStatus == status)
            && (!_from.Checked || date >= _from.Value.Date)
            && (!_to.Checked || date <= _to.Value.Date);
    }
}

