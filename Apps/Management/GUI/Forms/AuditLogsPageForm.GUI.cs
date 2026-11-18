namespace TrangSach.Management.GUI;

internal sealed class AuditLogsPageForm : ManagementGridPageForm
{
    private readonly ComboBox _area = ManagementGridPageForm.Choice(170, new ManagementSearchFilterOption(null, "Mọi chức năng"));
    private readonly DateTimePicker _from = ManagementGridPageForm.Date();
    private readonly DateTimePicker _to = ManagementGridPageForm.Date();

    public AuditLogsPageForm() : base("Nhật ký hệ thống")
    {
        SetSearchPlaceholder("Người thao tác, chức năng, nội dung, mã bản ghi...");
        _area.SelectedValueChanged += (_, _) => RefreshFilter();
        _from.ValueChanged += (_, _) => RefreshFilter();
        _to.ValueChanged += (_, _) => RefreshFilter();
        AddSearchFilterControl(ManagementGridPageForm.FilterLabel("Chức năng"));
        AddSearchFilterControl(_area);
        AddSearchFilterControl(ManagementGridPageForm.FilterLabel("Từ"));
        AddSearchFilterControl(_from);
        AddSearchFilterControl(ManagementGridPageForm.FilterLabel("Đến"));
        AddSearchFilterControl(_to);
        AddAction("Tải nhật ký", "load");
    }

    protected override bool HasAdditionalFilters => ManagementGridPageForm.Value(_area) is not null || _from.Checked || _to.Checked;

    protected override bool MatchesAdditionalFilters(object item)
    {
        if (item is not TrangSach.Shared.DTO.ManagerAuditLogDto log) return true;
        var area = ManagementGridPageForm.Value(_area);
        var date = log.OccurredAt.ToLocalTime().Date;
        return (area is null || log.Area == area)
            && (!_from.Checked || date >= _from.Value.Date)
            && (!_to.Checked || date <= _to.Value.Date);
    }

    protected override void OnDataSourceRefreshed(List<object> rows) =>
        ManagementGridPageForm.SetValues(_area, rows.OfType<TrangSach.Shared.DTO.ManagerAuditLogDto>().Select(log => log.Area), "Mọi chức năng");
}



