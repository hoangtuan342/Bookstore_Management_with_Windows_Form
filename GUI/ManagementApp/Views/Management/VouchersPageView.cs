namespace TrangSach.Management.Views;

internal sealed class VouchersPageView : ManagementGridPageView
{
    private readonly ComboBox _status = ManagementSearchFilterControls.Choice(160, new ManagementSearchFilterOption(null, "Mọi trạng thái"));
    private readonly ComboBox _validity = ManagementSearchFilterControls.Choice(190,
        new(null, "Mọi thời hạn"), new("valid", "Còn hạn và còn lượt"), new("expired", "Hết hạn hoặc hết lượt"));

    public VouchersPageView() : base("Voucher")
    {
        SetSearchPlaceholder("Mã voucher, chương trình áp dụng...");
        _status.SelectedValueChanged += (_, _) => RefreshFilter();
        _validity.SelectedValueChanged += (_, _) => RefreshFilter();
        AddSearchFilterControl(ManagementSearchFilterControls.Label("Trạng thái"));
        AddSearchFilterControl(_status);
        AddSearchFilterControl(_validity);
        AddAction("Tải", "load");
        AddAction("Cập nhật", "edit");
        AddAction("Xóa", "delete");
    }

    protected override bool HasAdditionalFilters => ManagementSearchFilterControls.Value(_status) is not null
        || ManagementSearchFilterControls.Value(_validity) is not null;

    protected override bool MatchesAdditionalFilters(object item)
    {
        if (item is not BookStore.Shared.Models.ManagerVoucher voucher) return true;
        var status = ManagementSearchFilterControls.Value(_status);
        var validity = ManagementSearchFilterControls.Value(_validity);
        var isValid = (voucher.ExpiresAt is null || voucher.ExpiresAt >= DateTime.Now)
            && (voucher.MaxUsageCount is null || voucher.UsageCount < voucher.MaxUsageCount);
        return (status is null || voucher.Status == status)
            && (validity is null || (validity == "valid" ? isValid : !isValid));
    }

    protected override IEnumerable<string?> GetAdditionalSearchValues(object item) => item is BookStore.Shared.Models.ManagerVoucher voucher
        ? voucher.Promotions
        : [];

    protected override void OnDataSourceRefreshed(IReadOnlyList<object> rows) =>
        ManagementSearchFilterControls.SetValues(_status, rows.OfType<BookStore.Shared.Models.ManagerVoucher>().Select(voucher => voucher.Status),
            "Mọi trạng thái", ManagementSearchFilterControls.StatusCaption);
}

