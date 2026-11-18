namespace TrangSach.Management.GUI;

internal sealed class EmployeesPageForm : ManagementGridPageForm
{
    private readonly ComboBox _employment = ManagementGridPageForm.Choice(145, new ManagementSearchFilterOption(null, "Mọi tình trạng"));
    private readonly ComboBox _role = ManagementGridPageForm.Choice(160, new ManagementSearchFilterOption(null, "Mọi vị trí"));
    private readonly ComboBox _login = ManagementGridPageForm.Choice(155, new ManagementSearchFilterOption(null, "Mọi quyền truy cập"));

    public EmployeesPageForm() : base("Nhân viên")
    {
        SetSearchPlaceholder("Tên, mã nhân viên, điện thoại, email...");
        _employment.SelectedValueChanged += (_, _) => RefreshFilter();
        _role.SelectedValueChanged += (_, _) => RefreshFilter();
        _login.SelectedValueChanged += (_, _) => RefreshFilter();
        AddSearchFilterControl(ManagementGridPageForm.FilterLabel("Công việc"));
        AddSearchFilterControl(_employment);
        AddSearchFilterControl(_role);
        AddSearchFilterControl(_login);
        AddAction("Tải", "load");
        AddAction("Thêm", "add");
        AddAction("Sửa", "edit");
        AddAction("Xóa", "delete");
    }

    protected override bool HasAdditionalFilters => ManagementGridPageForm.Value(_employment) is not null
        || ManagementGridPageForm.Value(_role) is not null || ManagementGridPageForm.Value(_login) is not null;

    protected override bool MatchesAdditionalFilters(object item)
    {
        if (item is not TrangSach.Shared.DTO.ManagerEmployeeDto employee) return true;
        var status = ManagementGridPageForm.Value(_employment);
        var role = ManagementGridPageForm.Value(_role);
        var login = ManagementGridPageForm.Value(_login);
        return (status is null || employee.AccountStatus == status)
            && (role is null || employee.RoleName == role)
            && (login is null || employee.LoginStatus == login);
    }

    protected override void OnDataSourceRefreshed(IReadOnlyList<object> rows)
    {
        var employees = rows.OfType<TrangSach.Shared.DTO.ManagerEmployeeDto>().ToArray();
        ManagementGridPageForm.SetValues(_employment, employees.Select(employee => employee.AccountStatus), "Mọi tình trạng", ManagementGridPageForm.StatusCaption);
        ManagementGridPageForm.SetValues(_role, employees.Select(employee => employee.RoleName), "Mọi vị trí");
        ManagementGridPageForm.SetValues(_login, employees.Select(employee => employee.LoginStatus), "Mọi quyền truy cập", ManagementGridPageForm.StatusCaption);
    }
}



