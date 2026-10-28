namespace TrangSach.Management.Views;

internal sealed class AuditLogsPageView : ManagementGridPageView
{
    private readonly ComboBox _area = ManagementSearchFilterControls.Choice(170, new ManagementSearchFilterOption(null, "Mọi chức năng"));
    private readonly DateTimePicker _from = ManagementSearchFilterControls.Date();
    private readonly DateTimePicker _to = ManagementSearchFilterControls.Date();

    public AuditLogsPageView() : base("Nhật ký hệ thống")
    {
        SetSearchPlaceholder("Người thao tác, chức năng, nội dung, mã bản ghi...");
        _area.SelectedValueChanged += (_, _) => RefreshFilter();
        _from.ValueChanged += (_, _) => RefreshFilter();
        _to.ValueChanged += (_, _) => RefreshFilter();
        AddSearchFilterControl(ManagementSearchFilterControls.Label("Chức năng"));
        AddSearchFilterControl(_area);
        AddSearchFilterControl(ManagementSearchFilterControls.Label("Từ"));
        AddSearchFilterControl(_from);
        AddSearchFilterControl(ManagementSearchFilterControls.Label("Đến"));
        AddSearchFilterControl(_to);
        AddAction("Tải nhật ký", "load");
    }

    protected override bool HasAdditionalFilters => ManagementSearchFilterControls.Value(_area) is not null || _from.Checked || _to.Checked;

    protected override bool MatchesAdditionalFilters(object item)
    {
        if (item is not BookStore.Shared.Models.ManagerAuditLog log) return true;
        var area = ManagementSearchFilterControls.Value(_area);
        var date = log.OccurredAt.ToLocalTime().Date;
        return (area is null || log.Area == area)
            && (!_from.Checked || date >= _from.Value.Date)
            && (!_to.Checked || date <= _to.Value.Date);
    }

    protected override void OnDataSourceRefreshed(IReadOnlyList<object> rows) =>
        ManagementSearchFilterControls.SetValues(_area, rows.OfType<BookStore.Shared.Models.ManagerAuditLog>().Select(log => log.Area), "Mọi chức năng");
}

