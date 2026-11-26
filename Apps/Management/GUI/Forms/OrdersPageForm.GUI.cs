using TrangSach.Shared.DTO;

namespace TrangSach.Management.GUI;

internal sealed class OrdersPageForm : ManagementGridPageForm
{
    private sealed record OrderStatusOption(string? Value, string Caption);
    private static readonly OrderStatusOption[] StatusOptions =
    [
        new(null, "Mọi trạng thái"),
        new("Pending", "Chờ xác nhận"),
        new("Confirmed", "Đã xác nhận"),
        new("Shipping", "Đang giao"),
        new("Completed", "Hoàn tất"),
        new("Cancelled", "Đã hủy")
    ];

    private readonly ComboBox _status = new();
    private readonly DateTimePicker _fromDate = CreateDateFilter();
    private readonly DateTimePicker _toDate = CreateDateFilter();

    public OrdersPageForm() : base("Hóa đơn")
    {
        SetSearchPlaceholder("Mã hóa đơn, khách hàng, nhân viên, tên sách...");
        _status.DropDownStyle = ComboBoxStyle.DropDownList;
        _status.Width = 130;
        _status.Font = new Font("Segoe UI", 8.5f);
        _status.DataSource = StatusOptions;
        _status.DisplayMember = nameof(OrderStatusOption.Caption);
        _status.SelectedIndexChanged += (_, _) => RefreshFilter();
        _fromDate.ValueChanged += (_, _) => RefreshFilter();
        _fromDate.CloseUp += (_, _) => RefreshFilter();
        _toDate.ValueChanged += (_, _) => RefreshFilter();
        _toDate.CloseUp += (_, _) => RefreshFilter();
        AddSearchFilterControl(new Label { Text = "Trạng thái", AutoSize = true, Margin = new Padding(3, 8, 2, 0), Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), ForeColor = Color.FromArgb(72, 88, 104) });
        AddSearchFilterControl(_status);
        AddSearchFilterControl(new Label { Text = "Từ ngày", AutoSize = true, Margin = new Padding(4, 8, 2, 0), Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), ForeColor = Color.FromArgb(72, 88, 104) });
        AddSearchFilterControl(_fromDate);
        AddSearchFilterControl(new Label { Text = "Đến ngày", AutoSize = true, Margin = new Padding(2, 8, 2, 0), Font = new Font("Segoe UI", 8.5f, FontStyle.Bold), ForeColor = Color.FromArgb(72, 88, 104) });
        AddSearchFilterControl(_toDate);

        var detailGrid = CreateDetailGrid();
        detailGrid.DataBindingComplete += (_, _) =>
        {
            try
            {
                if (detailGrid.Columns["BookId"] is { } bId) bId.HeaderText = "Mã sách";
                if (detailGrid.Columns["Title"] is { } title) title.HeaderText = "Tên sách";
                if (detailGrid.Columns["Quantity"] is { } qty)
                {
                    qty.HeaderText = "Số lượng";
                    qty.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                }
                if (detailGrid.Columns["UnitPrice"] is { } price)
                {
                    price.HeaderText = "Đơn giá (VNĐ)";
                    price.DefaultCellStyle.Format = "N0";
                    price.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                }
                if (detailGrid.Columns["LineTotal"] is { } total)
                {
                    total.HeaderText = "Thành tiền (VNĐ)";
                    total.DefaultCellStyle.Format = "N0";
                    total.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                }
            }
            catch { }
        };
        SetDetailsView(detailGrid, "CHI TIẾT HÓA ĐƠN · Chọn một hóa đơn phía trên để xem các mặt hàng");
        Grid.SelectionChanged += (_, _) =>
        {
            if (Grid.CurrentRow?.DataBoundItem is not ManagerOrderRowDto order)
            {
                detailGrid.DataSource = null;
                SetDetailsTitle("CHI TIẾT HÓA ĐƠN · Chọn một hóa đơn phía trên để xem các mặt hàng");
                return;
            }

            SetDetailsTitle($"CHI TIẾT HÓA ĐƠN {order.Id} · {order.CustomerName} · {order.ItemCount} mặt hàng · {order.TotalAmount:N0} ₫");
            detailGrid.DataSource = order.Lines.ToList();
        };
        AddAction("Tải", "load");
        AddAction("Tạo hóa đơn", "create");
        AddAction("In hóa đơn", "print");
        AddAction("Đổi trạng thái", "status");
        AddAction("Sửa thông tin", "edit");
        AddAction("Hủy hóa đơn", "cancel");

        Grid.CellFormatting += (_, e) =>
        {
            if (e.RowIndex < 0 || e.Value is null) return;
            var colName = Grid.Columns[e.ColumnIndex].DataPropertyName;
            if (colName == nameof(ManagerOrderRowDto.OrderStatus))
            {
                e.Value = OrderStatusFormatter.FormatOrderStatus(e.Value.ToString());
                e.FormattingApplied = true;
            }
            else if (colName == nameof(ManagerOrderRowDto.PaymentStatus))
            {
                e.Value = OrderStatusFormatter.FormatPaymentStatus(e.Value.ToString());
                e.FormattingApplied = true;
            }
            else if (colName == nameof(ManagerOrderRowDto.PaymentMethod))
            {
                e.Value = OrderStatusFormatter.FormatPaymentMethod(e.Value.ToString());
                e.FormattingApplied = true;
            }
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

            SetHeader(nameof(ManagerOrderRowDto.Id), "Mã hóa đơn");
            SetHeader(nameof(ManagerOrderRowDto.CreatedAt), "Ngày lập");
            SetHeader(nameof(ManagerOrderRowDto.CustomerName), "Khách hàng");
            SetHeader(nameof(ManagerOrderRowDto.EmployeeName), "Nhân viên lập");
            SetHeader(nameof(ManagerOrderRowDto.ReceiverPhone), "SĐT nhận hàng");
            SetHeader(nameof(ManagerOrderRowDto.ShippingAddress), "Địa chỉ giao hàng");
            SetHeader(nameof(ManagerOrderRowDto.PaymentMethod), "Phương thức TT");
            SetHeader(nameof(ManagerOrderRowDto.PaymentStatus), "Trạng thái TT");
            SetHeader(nameof(ManagerOrderRowDto.OrderStatus), "Trạng thái hóa đơn");
            SetHeader(nameof(ManagerOrderRowDto.ItemCount), "Số mặt hàng");
            SetHeader(nameof(ManagerOrderRowDto.TotalAmount), "Tổng tiền (VNĐ)");

            if (Grid.Columns[nameof(ManagerOrderRowDto.CreatedAt)] is { } createdCol)
            {
                createdCol.DefaultCellStyle.Format = "dd/MM/yyyy HH:mm";
            }
            if (Grid.Columns[nameof(ManagerOrderRowDto.ItemCount)] is { } countCol)
            {
                countCol.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            }
            if (Grid.Columns[nameof(ManagerOrderRowDto.TotalAmount)] is { } totalCol)
            {
                totalCol.DefaultCellStyle.Format = "N0";
                totalCol.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            }
            if (Grid.Columns[nameof(ManagerOrderRowDto.Lines)] is { } linesCol)
            {
                linesCol.Visible = false;
            }
        }
        catch { }
    }

    protected override bool HasAdditionalFilters =>
        (_status.SelectedItem as OrderStatusOption)?.Value is not null || _fromDate.Checked || _toDate.Checked;

    protected override bool MatchesAdditionalFilters(object item)
    {
        if (item is not ManagerOrderRowDto order) return true;
        var selectedStatus = (_status.SelectedItem as OrderStatusOption)?.Value;
        if (selectedStatus is not null && !string.Equals(order.OrderStatus, selectedStatus, StringComparison.OrdinalIgnoreCase)) return false;
        var createdDate = order.CreatedAt.ToLocalTime().Date;
        if (_fromDate.Checked && createdDate < _fromDate.Value.Date) return false;
        if (_toDate.Checked && createdDate > _toDate.Value.Date) return false;
        return true;
    }

    protected override IEnumerable<string?> GetAdditionalSearchValues(object item)
    {
        if (item is not ManagerOrderRowDto order) return [];
        return new[] { order.Id, order.CustomerName, order.EmployeeName, order.ReceiverPhone, order.OrderStatus,
            order.PaymentStatus, order.PaymentMethod, order.ShippingAddress }
            .Concat(order.Lines.SelectMany(line => new[] { line.BookId, line.Title }));
    }

    private static DateTimePicker CreateDateFilter() => new()
    {
        Format = DateTimePickerFormat.Short,
        ShowCheckBox = true,
        Checked = false,
        Width = 112,
        Font = new Font("Segoe UI", 8.5f),
        Margin = new Padding(2, 3, 4, 0)
    };
}




