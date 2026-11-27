namespace TrangSach.Management.GUI;

internal sealed class EmployeesPageForm : ManagementGridPageForm
{
    private readonly ComboBox _employment = ManagementGridPageForm.Choice(145, new ManagementSearchFilterOption(null, "Mọi tình trạng"));
    private readonly ComboBox _role = ManagementGridPageForm.Choice(160, new ManagementSearchFilterOption(null, "Mọi vị trí"));
    private readonly ComboBox _login = ManagementGridPageForm.Choice(155, new ManagementSearchFilterOption(null, "Mọi quyền truy cập"));

    public sealed record EmployeeAccountDetailDto(
        int? AccountId,
        string? UserName,
        string? Email,
        string? RoleName,
        string? LoginStatus,
        string StatusText
    );

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

        var detailGrid = CreateDetailGrid();
        detailGrid.DataBindingComplete += (_, _) =>
        {
            try
            {
                if (detailGrid.Columns[nameof(EmployeeAccountDetailDto.AccountId)] is { } idCol)
                {
                    idCol.HeaderText = "Mã tài khoản";
                    idCol.Width = 110;
                }
                if (detailGrid.Columns[nameof(EmployeeAccountDetailDto.UserName)] is { } userCol)
                {
                    userCol.HeaderText = "Tên đăng nhập";
                    userCol.Width = 170;
                }
                if (detailGrid.Columns[nameof(EmployeeAccountDetailDto.Email)] is { } emailCol)
                {
                    emailCol.HeaderText = "Email";
                    emailCol.Width = 230;
                }
                if (detailGrid.Columns[nameof(EmployeeAccountDetailDto.RoleName)] is { } roleCol)
                {
                    roleCol.HeaderText = "Vai trò / Chức vụ";
                    roleCol.Width = 160;
                }
                if (detailGrid.Columns[nameof(EmployeeAccountDetailDto.StatusText)] is { } statusCol)
                {
                    statusCol.HeaderText = "Trạng thái đăng nhập";
                    statusCol.Width = 170;
                }
                if (detailGrid.Columns[nameof(EmployeeAccountDetailDto.LoginStatus)] is { } rawCol)
                {
                    rawCol.Visible = false;
                }
            }
            catch { }
        };

        SetDetailsView(detailGrid, "TÀI KHOẢN NHÂN VIÊN · Chọn một nhân viên ở bảng trên để xem thông tin tài khoản");

        Grid.SelectionChanged += (_, _) =>
        {
            if (Grid.CurrentRow?.DataBoundItem is not TrangSach.Shared.DTO.ManagerEmployeeDto employee)
            {
                detailGrid.DataSource = null;
                SetDetailsTitle("TÀI KHOẢN NHÂN VIÊN · Chọn một nhân viên ở bảng trên để xem thông tin tài khoản");
                return;
            }

            var fullName = $"{employee.LastName} {employee.FirstName}".Trim();
            if (employee.AccountId is null && string.IsNullOrWhiteSpace(employee.UserName))
            {
                SetDetailsTitle($"TÀI KHOẢN NHÂN VIÊN · {fullName} ({employee.Id}) · Chưa có tài khoản đăng nhập");
                detailGrid.DataSource = new List<EmployeeAccountDetailDto>();
                return;
            }

            SetDetailsTitle($"TÀI KHOẢN NHÂN VIÊN · {fullName} ({employee.Id}) · Tài khoản: {employee.UserName ?? "Chưa đặt"} · Quyền: {employee.RoleName}");
            var displayStatus = ManagementGridPageForm.StatusCaption(employee.LoginStatus ?? "Active");
            detailGrid.DataSource = new List<EmployeeAccountDetailDto>
            {
                new(
                    employee.AccountId,
                    employee.UserName ?? "(Chưa có)",
                    employee.Email ?? "(Chưa có)",
                    employee.RoleName,
                    employee.LoginStatus ?? "Active",
                    displayStatus
                )
            };
        };
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

            // Ẩn các cột tài khoản ở bảng trên vì đã hiển thị ở bảng tài khoản chi tiết bên dưới
            if (Grid.Columns[nameof(TrangSach.Shared.DTO.ManagerEmployeeDto.AccountId)] is { } accIdCol)
            {
                accIdCol.Visible = false;
            }
            if (Grid.Columns[nameof(TrangSach.Shared.DTO.ManagerEmployeeDto.UserName)] is { } userCol)
            {
                userCol.Visible = false;
            }
            if (Grid.Columns[nameof(TrangSach.Shared.DTO.ManagerEmployeeDto.Email)] is { } emailCol)
            {
                emailCol.Visible = false;
            }
            if (Grid.Columns[nameof(TrangSach.Shared.DTO.ManagerEmployeeDto.LoginStatus)] is { } loginCol)
            {
                loginCol.Visible = false;
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



