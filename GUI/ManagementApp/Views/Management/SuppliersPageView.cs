namespace TrangSach.Management.Views;

internal sealed class SuppliersPageView : ManagementGridPageView
{
    private readonly ComboBox _phone = ManagementSearchFilterControls.Choice(170,
        new(null, "Mọi nhà cung cấp"), new("yes", "Có số điện thoại"), new("no", "Chưa có số điện thoại"));

    public SuppliersPageView() : base("Nhà cung cấp")
    {
        SetSearchPlaceholder("Tên, mã nhà cung cấp, số điện thoại...");
        _phone.SelectedValueChanged += (_, _) => RefreshFilter();
        AddSearchFilterControl(ManagementSearchFilterControls.Label("Liên hệ"));
        AddSearchFilterControl(_phone);
        AddAction("Tải", "load");
        AddAction("Thêm", "add");
        AddAction("Sửa", "edit");
        AddAction("Xóa", "delete");
    }

    protected override bool HasAdditionalFilters => ManagementSearchFilterControls.Value(_phone) is not null;
    protected override bool MatchesAdditionalFilters(object item) => item is not BookStore.Shared.Models.SupplierOption supplier
        || ManagementSearchFilterControls.Value(_phone) switch
        {
            "yes" => !string.IsNullOrWhiteSpace(supplier.PhoneNumber),
            "no" => string.IsNullOrWhiteSpace(supplier.PhoneNumber),
            _ => true
        };
}

