namespace TrangSach.Management.GUI;

internal sealed class CustomersPageForm : ManagementGridPageForm
{
    private readonly ComboBox _accountStatus = ManagementGridPageForm.Choice(160, new ManagementSearchFilterOption(null, "Mọi trạng thái tài khoản"));
    private readonly ComboBox _account = ManagementGridPageForm.Choice(155,
        new ManagementSearchFilterOption(null, "Tất cả khách hàng"), new ManagementSearchFilterOption("yes", "Có tài khoản"),
        new ManagementSearchFilterOption("no", "Chưa có tài khoản"));

    public CustomersPageForm() : base("Khách hàng")
    {
        SetSearchPlaceholder("Tên, mã khách hàng, điện thoại, email, địa chỉ...");
        _accountStatus.SelectedValueChanged += (_, _) => RefreshFilter();
        _account.SelectedValueChanged += (_, _) => RefreshFilter();
        AddSearchFilterControl(ManagementGridPageForm.FilterLabel("Tài khoản"));
        AddSearchFilterControl(_accountStatus);
        AddSearchFilterControl(_account);
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

            SetHeader(nameof(TrangSach.Shared.DTO.ManagerCustomerDto.Id), "Mã khách hàng");
            SetHeader(nameof(TrangSach.Shared.DTO.ManagerCustomerDto.FullName), "Họ và tên");
            SetHeader(nameof(TrangSach.Shared.DTO.ManagerCustomerDto.BirthDate), "Ngày sinh");
            SetHeader(nameof(TrangSach.Shared.DTO.ManagerCustomerDto.PhoneNumber), "Số điện thoại");
            SetHeader(nameof(TrangSach.Shared.DTO.ManagerCustomerDto.Address), "Địa chỉ");
            SetHeader(nameof(TrangSach.Shared.DTO.ManagerCustomerDto.UserName), "Tên đăng nhập");
            SetHeader(nameof(TrangSach.Shared.DTO.ManagerCustomerDto.Email), "Email");
            SetHeader(nameof(TrangSach.Shared.DTO.ManagerCustomerDto.AccountStatus), "Trạng thái tài khoản");

            if (Grid.Columns[nameof(TrangSach.Shared.DTO.ManagerCustomerDto.BirthDate)] is { } birthCol)
            {
                birthCol.DefaultCellStyle.Format = "dd/MM/yyyy";
            }

            if (Grid.Columns[nameof(TrangSach.Shared.DTO.ManagerCustomerDto.AccountId)] is { } accIdCol)
            {
                accIdCol.Visible = false;
            }
        }
        catch { }
    }

    protected override bool HasAdditionalFilters => ManagementGridPageForm.Value(_accountStatus) is not null
        || ManagementGridPageForm.Value(_account) is not null;
    protected override bool MatchesAdditionalFilters(object item)
    {
        if (item is not TrangSach.Shared.DTO.ManagerCustomerDto customer) return true;
        var status = ManagementGridPageForm.Value(_accountStatus);
        var account = ManagementGridPageForm.Value(_account);
        return (status is null || string.Equals(customer.AccountStatus, status, StringComparison.OrdinalIgnoreCase))
            && (account is null || (account == "yes" ? customer.AccountId is not null : customer.AccountId is null));
    }

    protected override void OnDataSourceRefreshed(List<object> rows) =>
        ManagementGridPageForm.SetValues(_accountStatus, rows.OfType<TrangSach.Shared.DTO.ManagerCustomerDto>().Select(customer => customer.AccountStatus),
            "Mọi trạng thái tài khoản", ManagementGridPageForm.StatusCaption);
}



