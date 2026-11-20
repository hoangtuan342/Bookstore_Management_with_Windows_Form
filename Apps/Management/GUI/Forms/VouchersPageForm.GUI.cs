namespace TrangSach.Management.GUI;

internal sealed class VouchersPageForm : ManagementGridPageForm
{
    private readonly ComboBox _status = ManagementGridPageForm.Choice(160, new ManagementSearchFilterOption(null, "Mọi trạng thái"));
    private readonly ComboBox _validity = ManagementGridPageForm.Choice(190,
        new(null, "Mọi thời hạn"), new("valid", "Còn hạn và còn lượt"), new("expired", "Hết hạn hoặc hết lượt"));

    public VouchersPageForm() : base("Voucher")
    {
        SetSearchPlaceholder("Mã voucher, chương trình áp dụng...");
        _status.SelectedValueChanged += (_, _) => RefreshFilter();
        _validity.SelectedValueChanged += (_, _) => RefreshFilter();
        AddSearchFilterControl(ManagementGridPageForm.FilterLabel("Trạng thái"));
        AddSearchFilterControl(_status);
        AddSearchFilterControl(_validity);
        AddAction("Tải", "load");
        AddAction("Thêm", "create");
        AddAction("Cập nhật", "edit");
        AddAction("Xóa", "delete");
    }

    protected override bool HasAdditionalFilters => ManagementGridPageForm.Value(_status) is not null
        || ManagementGridPageForm.Value(_validity) is not null;

    protected override bool MatchesAdditionalFilters(object item)
    {
        if (item is not TrangSach.Shared.DTO.ManagerVoucherDto voucher) return true;
        var status = ManagementGridPageForm.Value(_status);
        var validity = ManagementGridPageForm.Value(_validity);
        var isValid = (voucher.ExpiresAt is null || voucher.ExpiresAt >= DateTime.Now)
            && (voucher.MaxUsageCount is null || voucher.UsageCount < voucher.MaxUsageCount);
        return (status is null || voucher.Status == status)
            && (validity is null || (validity == "valid" ? isValid : !isValid));
    }

    protected override IEnumerable<string?> GetAdditionalSearchValues(object item) => item is TrangSach.Shared.DTO.ManagerVoucherDto voucher
        ? voucher.Promotions
        : [];

    protected override void OnDataSourceRefreshed(List<object> rows) =>
        ManagementGridPageForm.SetValues(_status, rows.OfType<TrangSach.Shared.DTO.ManagerVoucherDto>().Select(voucher => voucher.Status),
            "Mọi trạng thái", ManagementGridPageForm.StatusCaption);
}



