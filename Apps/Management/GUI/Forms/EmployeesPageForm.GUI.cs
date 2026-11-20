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

    protected override void OnGridDataBound()
    {
        base.OnGridDataBound();
        try
        {
            void SetHeader(string propertyName, string headerText)
            {
                if (Grid.Columns[propertyName] is { } col)
                {
                    col.HeaderText = headerText;
                }
            }

            SetHeader(nameof(TrangSach.Shared.DTO.ManagerEmployeeDto.Id), "Mã nhân viên");
            SetHeader(nameof(TrangSach.Shared.DTO.ManagerEmployeeDto.LastName), "Họ lót");
            SetHeader(nameof(TrangSach.Shared.DTO.ManagerEmployeeDto.FirstName), "Tên");
            SetHeader(nameof(TrangSach.Shared.DTO.ManagerEmployeeDto.BirthDate), "Ngày sinh");
            SetHeader(nameof(TrangSach.Shared.DTO.ManagerEmployeeDto.Address), "Địa chỉ");
            SetHeader(nameof(TrangSach.Shared.DTO.ManagerEmployeeDto.RoleName), "Chức vụ");
            SetHeader(nameof(TrangSach.Shared.DTO.ManagerEmployeeDto.BaseSalary), "Lương cơ bản (VNĐ)");
            SetHeader(nameof(TrangSach.Shared.DTO.ManagerEmployeeDto.AccountStatus), "Trạng thái hồ sơ");
            SetHeader(nameof(TrangSach.Shared.DTO.ManagerEmployeeDto.LoginStatus), "Quyền truy cập");
            SetHeader(nameof(TrangSach.Shared.DTO.ManagerEmployeeDto.UserName), "Tên tài khoản");
            SetHeader(nameof(TrangSach.Shared.DTO.ManagerEmployeeDto.Email), "Email");

            if (Grid.Columns[nameof(TrangSach.Shared.DTO.ManagerEmployeeDto.BaseSalary)] is { } salCol)
            {
                salCol.DefaultCellStyle.Format = "N0";
                salCol.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            }
            if (Grid.Columns[nameof(TrangSach.Shared.DTO.ManagerEmployeeDto.BirthDate)] is { } birthCol)
            {
                birthCol.DefaultCellStyle.Format = "dd/MM/yyyy";
            }
            if (Grid.Columns[nameof(TrangSach.Shared.DTO.ManagerEmployeeDto.AvatarPath)] is { } avaCol)
            {
                avaCol.Visible = false;
            }
            if (Grid.Columns[nameof(TrangSach.Shared.DTO.ManagerEmployeeDto.AccountId)] is { } accIdCol)
            {
                accIdCol.Visible = false;
            }
        }
        catch { }
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

    protected override void OnDataSourceRefreshed(List<object> rows)
    {
        var employees = rows.OfType<TrangSach.Shared.DTO.ManagerEmployeeDto>().ToArray();
        ManagementGridPageForm.SetValues(_employment, employees.Select(employee => employee.AccountStatus), "Mọi tình trạng", ManagementGridPageForm.StatusCaption);
        ManagementGridPageForm.SetValues(_role, employees.Select(employee => employee.RoleName), "Mọi vị trí");
        ManagementGridPageForm.SetValues(_login, employees.Select(employee => employee.LoginStatus), "Mọi quyền truy cập", ManagementGridPageForm.StatusCaption);
    }
}



