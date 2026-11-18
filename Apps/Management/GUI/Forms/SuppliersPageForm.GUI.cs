namespace TrangSach.Management.GUI;

internal sealed class SuppliersPageForm : ManagementGridPageForm
{
    private readonly ComboBox _phone = ManagementGridPageForm.Choice(170,
        new(null, "Mọi nhà cung cấp"), new("yes", "Có số điện thoại"), new("no", "Chưa có số điện thoại"));

    public SuppliersPageForm() : base("Nhà cung cấp")
    {
        SetSearchPlaceholder("Tên, mã nhà cung cấp, số điện thoại...");
        _phone.SelectedValueChanged += (_, _) => RefreshFilter();
        AddSearchFilterControl(ManagementGridPageForm.FilterLabel("Liên hệ"));
        AddSearchFilterControl(_phone);
        AddAction("Tải", "load");
        AddAction("Thêm", "add");
        AddAction("Sửa", "edit");
        AddAction("Xóa", "delete");
    }

    protected override bool HasAdditionalFilters => ManagementGridPageForm.Value(_phone) is not null;
    protected override bool MatchesAdditionalFilters(object item) => item is not TrangSach.Shared.DTO.SupplierOptionDto supplier
        || ManagementGridPageForm.Value(_phone) switch
        {
            "yes" => !string.IsNullOrWhiteSpace(supplier.PhoneNumber),
            "no" => string.IsNullOrWhiteSpace(supplier.PhoneNumber),
            _ => true
        };
}



