namespace TrangSach.Management.GUI;

internal sealed class CustomersPageForm : ManagementGridPageForm
{
    private readonly ComboBox _accountStatus = ManagementGridPageForm.Choice(160, new ManagementSearchFilterOption(null, "Mọi trạng thái tài khoản"));
    private readonly ComboBox _account = ManagementGridPageForm.Choice(155,
        new ManagementSearchFilterOption(null, "Tất cả khách hàng"), new ManagementSearchFilterOption("yes", "Có tài khoản"),
        new ManagementSearchFilterOption("no", "Chưa có tài khoản"));

    public sealed record CustomerAccountDetailDto(
        int? AccountId,
        string? UserName,
        string? Email,
        string? AccountStatus,
        string StatusText
    );

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

        var detailGrid = CreateDetailGrid();
        detailGrid.DataBindingComplete += (_, _) =>
        {
            try
            {
                if (detailGrid.Columns[nameof(CustomerAccountDetailDto.AccountId)] is { } idCol)
                {
                    idCol.HeaderText = "Mã tài khoản";
                    idCol.Width = 110;
                }
                if (detailGrid.Columns[nameof(CustomerAccountDetailDto.UserName)] is { } userCol)
                {
                    userCol.HeaderText = "Tên đăng nhập";
                    userCol.Width = 180;
                }
                if (detailGrid.Columns[nameof(CustomerAccountDetailDto.Email)] is { } emailCol)
                {
                    emailCol.HeaderText = "Email";
                    emailCol.Width = 240;
                }
                if (detailGrid.Columns[nameof(CustomerAccountDetailDto.StatusText)] is { } statusCol)
                {
                    statusCol.HeaderText = "Trạng thái đăng nhập";
                    statusCol.Width = 180;
                }
                if (detailGrid.Columns[nameof(CustomerAccountDetailDto.AccountStatus)] is { } rawCol)
                {
                    rawCol.Visible = false;
                }
            }
            catch { }
        };

        SetDetailsView(detailGrid, "TÀI KHOẢN KHÁCH HÀNG · Chọn một khách hàng ở bảng trên để xem thông tin tài khoản");

        Grid.SelectionChanged += (_, _) =>
        {
            if (Grid.CurrentRow?.DataBoundItem is not TrangSach.Shared.DTO.ManagerCustomerDto customer)
            {
                detailGrid.DataSource = null;
                SetDetailsTitle("TÀI KHOẢN KHÁCH HÀNG · Chọn một khách hàng ở bảng trên để xem thông tin tài khoản");
                return;
            }

            if (customer.AccountId is null && string.IsNullOrWhiteSpace(customer.UserName))
            {
                SetDetailsTitle($"TÀI KHOẢN KHÁCH HÀNG · {customer.FullName} ({customer.Id}) · Chưa có tài khoản đăng nhập");
                detailGrid.DataSource = new List<CustomerAccountDetailDto>();
                return;
            }

            SetDetailsTitle($"TÀI KHOẢN KHÁCH HÀNG · {customer.FullName} ({customer.Id}) · Tài khoản: {customer.UserName ?? "Chưa đặt"}");
            var displayStatus = ManagementGridPageForm.StatusCaption(customer.AccountStatus ?? "Active");
            detailGrid.DataSource = new List<CustomerAccountDetailDto>
            {
                new(
                    customer.AccountId,
                    customer.UserName ?? "(Chưa có)",
                    customer.Email ?? "(Chưa có)",
                    customer.AccountStatus ?? "Active",
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

            SetHeader(nameof(TrangSach.Shared.DTO.ManagerCustomerDto.Id), "Mã khách hàng");
            SetHeader(nameof(TrangSach.Shared.DTO.ManagerCustomerDto.FullName), "Họ và tên");
            SetHeader(nameof(TrangSach.Shared.DTO.ManagerCustomerDto.BirthDate), "Ngày sinh");
            SetHeader(nameof(TrangSach.Shared.DTO.ManagerCustomerDto.PhoneNumber), "Số điện thoại");
            SetHeader(nameof(TrangSach.Shared.DTO.ManagerCustomerDto.Address), "Địa chỉ");

            if (Grid.Columns[nameof(TrangSach.Shared.DTO.ManagerCustomerDto.BirthDate)] is { } birthCol)
            {
                birthCol.DefaultCellStyle.Format = "dd/MM/yyyy";
            }

            // Ẩn các cột tài khoản ở bảng trên vì đã hiển thị ở bảng tài khoản chi tiết bên dưới
            if (Grid.Columns[nameof(TrangSach.Shared.DTO.ManagerCustomerDto.AccountId)] is { } accIdCol)
            {
                accIdCol.Visible = false;
            }
            if (Grid.Columns[nameof(TrangSach.Shared.DTO.ManagerCustomerDto.UserName)] is { } userCol)
            {
                userCol.Visible = false;
            }
            if (Grid.Columns[nameof(TrangSach.Shared.DTO.ManagerCustomerDto.Email)] is { } emailCol)
            {
                emailCol.Visible = false;
            }
            if (Grid.Columns[nameof(TrangSach.Shared.DTO.ManagerCustomerDto.AccountStatus)] is { } statusCol)
            {
                statusCol.Visible = false;
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



