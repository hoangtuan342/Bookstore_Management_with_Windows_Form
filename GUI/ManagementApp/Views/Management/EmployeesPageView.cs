namespace TrangSach.Management.Views;

internal sealed class EmployeesPageView : ManagementGridPageView
{
    private readonly ComboBox _employment = ManagementSearchFilterControls.Choice(145, new ManagementSearchFilterOption(null, "Mọi tình trạng"));
    private readonly ComboBox _role = ManagementSearchFilterControls.Choice(160, new ManagementSearchFilterOption(null, "Mọi vị trí"));
    private readonly ComboBox _login = ManagementSearchFilterControls.Choice(155, new ManagementSearchFilterOption(null, "Mọi quyền truy cập"));

    public EmployeesPageView() : base("Nhân viên")
    {
        SetSearchPlaceholder("Tên, mã nhân viên, điện thoại, email...");
        _employment.SelectedValueChanged += (_, _) => RefreshFilter();
        _role.SelectedValueChanged += (_, _) => RefreshFilter();
        _login.SelectedValueChanged += (_, _) => RefreshFilter();
        AddSearchFilterControl(ManagementSearchFilterControls.Label("Công việc"));
        AddSearchFilterControl(_employment);
        AddSearchFilterControl(_role);
        AddSearchFilterControl(_login);
        AddAction("Tải", "load");
        AddAction("Thêm", "add");
        AddAction("Sửa", "edit");
        AddAction("Xóa", "delete");
    }

    protected override bool HasAdditionalFilters => ManagementSearchFilterControls.Value(_employment) is not null
        || ManagementSearchFilterControls.Value(_role) is not null || ManagementSearchFilterControls.Value(_login) is not null;

    protected override bool MatchesAdditionalFilters(object item)
    {
        if (item is not BookStore.Shared.Models.ManagerEmployee employee) return true;
        var status = ManagementSearchFilterControls.Value(_employment);
        var role = ManagementSearchFilterControls.Value(_role);
        var login = ManagementSearchFilterControls.Value(_login);
        return (status is null || employee.AccountStatus == status)
            && (role is null || employee.RoleName == role)
            && (login is null || employee.LoginStatus == login);
    }

    protected override void OnDataSourceRefreshed(IReadOnlyList<object> rows)
    {
        var employees = rows.OfType<BookStore.Shared.Models.ManagerEmployee>().ToArray();
        ManagementSearchFilterControls.SetValues(_employment, employees.Select(employee => employee.AccountStatus), "Mọi tình trạng", ManagementSearchFilterControls.StatusCaption);
        ManagementSearchFilterControls.SetValues(_role, employees.Select(employee => employee.RoleName), "Mọi vị trí");
        ManagementSearchFilterControls.SetValues(_login, employees.Select(employee => employee.LoginStatus), "Mọi quyền truy cập", ManagementSearchFilterControls.StatusCaption);
    }
}

