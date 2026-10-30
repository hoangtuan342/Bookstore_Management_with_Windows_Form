namespace TrangSach.Management.Views;

internal sealed class CustomersPageView : ManagementGridPageView
{
    private readonly ComboBox _accountStatus = ManagementSearchFilterControls.Choice(160, new ManagementSearchFilterOption(null, "Mọi trạng thái tài khoản"));
    private readonly ComboBox _account = ManagementSearchFilterControls.Choice(155,
        new ManagementSearchFilterOption(null, "Tất cả khách hàng"), new ManagementSearchFilterOption("yes", "Có tài khoản"),
        new ManagementSearchFilterOption("no", "Chưa có tài khoản"));

    public CustomersPageView() : base("Khách hàng")
    {
        SetSearchPlaceholder("Tên, mã khách hàng, điện thoại, email, địa chỉ...");
        _accountStatus.SelectedValueChanged += (_, _) => RefreshFilter();
        _account.SelectedValueChanged += (_, _) => RefreshFilter();
        AddSearchFilterControl(ManagementSearchFilterControls.Label("Tài khoản"));
        AddSearchFilterControl(_accountStatus);
        AddSearchFilterControl(_account);
        AddAction("Tải", "load");
        AddAction("Thêm", "add");
        AddAction("Sửa", "edit");
        AddAction("Xóa", "delete");
    }

    protected override bool HasAdditionalFilters => ManagementSearchFilterControls.Value(_accountStatus) is not null
        || ManagementSearchFilterControls.Value(_account) is not null;
    protected override bool MatchesAdditionalFilters(object item)
    {
        if (item is not BookStore.Shared.Models.ManagerCustomer customer) return true;
        var status = ManagementSearchFilterControls.Value(_accountStatus);
        var account = ManagementSearchFilterControls.Value(_account);
        return (status is null || string.Equals(customer.AccountStatus, status, StringComparison.OrdinalIgnoreCase))
            && (account is null || (account == "yes" ? customer.AccountId is not null : customer.AccountId is null));
    }

    protected override void OnDataSourceRefreshed(IReadOnlyList<object> rows) =>
        ManagementSearchFilterControls.SetValues(_accountStatus, rows.OfType<BookStore.Shared.Models.ManagerCustomer>().Select(customer => customer.AccountStatus),
            "Mọi trạng thái tài khoản", ManagementSearchFilterControls.StatusCaption);
}

